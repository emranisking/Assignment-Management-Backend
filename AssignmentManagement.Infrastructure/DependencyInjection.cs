using AssignmentManagement.Application.Common.Interfaces;
using AssignmentManagement.Application.Enrollments.Messages;
using AssignmentManagement.Infrastructure.Authentication;
using AssignmentManagement.Infrastructure.Caching;
using AssignmentManagement.Infrastructure.Messaging;
using AssignmentManagement.Infrastructure.Persistence;
using AssignmentManagement.Infrastructure.Persistence.Seed;
using AssignmentManagement.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssignmentManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // ---- Database ----
        var connectionString = config.GetConnectionString("Postgres")
                               ?? "Host=localhost;Port=5432;Database=assignment_management;Username=postgres;Password=postgres";

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // ---- Auth ----
        services.Configure<JwtOptions>(config.GetSection("Jwt"));
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        // ---- File storage ----
        var storageOptions = config.GetSection("Storage").Get<FileStorageOptions>() ?? new FileStorageOptions();
        services.AddSingleton(storageOptions);
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();

        // ---- Cache (Redis, falls back to in-memory distributed cache) ----
        var cacheOptions = config.GetSection("Cache").Get<CacheOptions>() ?? new CacheOptions();
        // Allow the global expiry to come straight from the environment.
        var envExpiry = config["Cache:ExpirySeconds"];
        if (int.TryParse(envExpiry, out var seconds)) cacheOptions.ExpirySeconds = seconds;
        services.AddSingleton(cacheOptions);

        var redisConnection = config["Redis:ConnectionString"];
        if (cacheOptions.Enabled && !string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddStackExchangeRedisCache(o =>
            {
                o.Configuration = redisConnection;
                o.InstanceName = "am:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }
        services.AddScoped<ICacheService, RedisCacheService>();

        // ---- Messaging (RabbitMQ) + enrollment processing mode ----
        var rabbitOptions = config.GetSection("RabbitMq").Get<RabbitMqOptions>() ?? new RabbitMqOptions();
        var enrollmentOptions = config.GetSection("Enrollment").Get<EnrollmentOptions>() ?? new EnrollmentOptions();
        enrollmentOptions.QueueName = rabbitOptions.QueueName;

        var useAsyncSetting = config.GetValue<bool?>("Enrollment:UseAsyncProcessing");
        enrollmentOptions.UseAsyncProcessing = (useAsyncSetting ?? rabbitOptions.Enabled) && rabbitOptions.Enabled;

        services.AddSingleton(rabbitOptions);
        services.AddSingleton(enrollmentOptions);

        if (rabbitOptions.Enabled)
        {
            services.AddSingleton<RabbitMqConnection>();
            services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
            services.AddHostedService<EnrollmentConsumer>();
        }
        else
        {
            services.AddSingleton<IMessagePublisher, NoOpMessagePublisher>();
        }

        // ---- Seeder ----
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
