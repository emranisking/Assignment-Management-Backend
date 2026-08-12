using System.Text;
using System.Text.Json;
using AssignmentManagement.Application.Common.Interfaces;
using AssignmentManagement.Application.Enrollments.Messages;
using AssignmentManagement.Application.Enrollments.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AssignmentManagement.Infrastructure.Messaging;

/// <summary>
/// Background worker that consumes enrollment messages and runs the pessimistic-locking
/// transaction. It acknowledges the RabbitMQ message only after the DB transaction commits.
/// </summary>
public class EnrollmentConsumer : BackgroundService
{
    private readonly RabbitMqConnection _connection;
    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EnrollmentConsumer> _logger;
    private IModel? _channel;

    public EnrollmentConsumer(
        RabbitMqConnection connection,
        RabbitMqOptions options,
        IServiceScopeFactory scopeFactory,
        ILogger<EnrollmentConsumer> logger)
    {
        _connection = connection;
        _options = options;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Retry connecting until the broker is available (it may still be starting up).
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _channel = _connection.CreateChannel();
                _channel.QueueDeclare(_options.QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
                _channel.BasicQos(prefetchSize: 0, prefetchCount: 5, global: false);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += OnReceivedAsync;
                _channel.BasicConsume(_options.QueueName, autoAck: false, consumer);

                _logger.LogInformation("EnrollmentConsumer connected and listening on '{Queue}'.", _options.QueueName);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "EnrollmentConsumer could not connect to RabbitMQ; retrying in 10s...");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        // Keep the service alive until shutdown.
        while (!stoppingToken.IsCancellationRequested)
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
    }

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        var channel = _channel!;
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var message = JsonSerializer.Deserialize<EnrollmentMessage>(json);
            if (message is null)
            {
                _logger.LogWarning("Received an unparsable enrollment message; discarding.");
                channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IEnrollmentProcessor>();
            await processor.ProcessAsync(message.RequestId);

            // ACK only after the DB transaction has committed inside ProcessAsync.
            channel.BasicAck(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process enrollment message; sending to dead path (no requeue).");
            // requeue:false to avoid poison-message loops. A DLQ is the natural next step.
            channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
        }
    }

    public override void Dispose()
    {
        try { _channel?.Close(); _channel?.Dispose(); } catch { /* ignore */ }
        base.Dispose();
    }
}
