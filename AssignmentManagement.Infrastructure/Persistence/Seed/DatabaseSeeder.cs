using AssignmentManagement.Application.Common.Interfaces;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AssignmentManagement.Infrastructure.Persistence.Seed;

/// <summary>Seeds a default admin plus sample users/course/class so the API is usable immediately.</summary>
public class DatabaseSeeder
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(AppDbContext db, IPasswordHasher hasher, ILogger<DatabaseSeeder> logger)
    {
        _db = db;
        _hasher = hasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await _db.Users.AnyAsync(ct))
        {
            _logger.LogInformation("Database already seeded; skipping.");
            return;
        }

        _logger.LogInformation("Seeding initial data...");

        var admin = new User { Name = "System Admin", Email = "admin@example.com", Role = UserRole.Admin, IsActive = true, PasswordHash = _hasher.Hash("Admin@123") };
        var teacher = new User { Name = "Demo Teacher", Email = "teacher@example.com", Role = UserRole.Teacher, IsActive = true, PasswordHash = _hasher.Hash("Teacher@123") };
        var student = new User { Name = "Demo Student", Email = "student@example.com", Role = UserRole.Student, IsActive = true, PasswordHash = _hasher.Hash("Student@123") };
        var student2 = new User { Name = "Second Student", Email = "student2@example.com", Role = UserRole.Student, IsActive = true, PasswordHash = _hasher.Hash("Student@123") };

        _db.Users.AddRange(admin, teacher, student, student2);
        await _db.SaveChangesAsync(ct);

        var course = new Course { Code = "CSE101", Name = "Database Management", Description = "Intro to relational databases.", CreditHours = 3 };
        _db.Courses.Add(course);
        await _db.SaveChangesAsync(ct);

        var cls = new Class
        {
            CourseId = course.Id,
            TeacherId = teacher.Id,
            Name = "Section A",
            DayOfWeek = DayOfWeek.Sunday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(12, 0),
            Capacity = 2,
            EnrolledCount = 0,
            EnrollmentDeadline = DateTime.UtcNow.AddDays(30),
            Status = ClassStatus.Open
        };
        _db.Classes.Add(cls);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Seed complete. Admin: admin@example.com / Admin@123");
    }
}
