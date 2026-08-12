using AssignmentManagement.Application.Enrollments.Processors;
using AssignmentManagement.Domain.Entities;
using AssignmentManagement.Domain.Enums;
using AssignmentManagement.Infrastructure.Persistence;
using AssignmentManagement.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AssignmentManagement.Tests.Enrollments;

public class EnrollmentProcessorTests
{
    private static async Task<(AppDbContext db, long classId, long studentId)> SeedAsync(
        int capacity, int enrolledCount)
    {
        var db = TestHelpers.NewInMemoryDb();

        var student = new User { Name = "S", Email = "s@x.com", Role = UserRole.Student, PasswordHash = "x" };
        var course = new Course { Code = "C1", Name = "Course" };
        db.Users.Add(student);
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var cls = new Class
        {
            CourseId = course.Id,
            Name = "A",
            Capacity = capacity,
            EnrolledCount = enrolledCount,
            EnrollmentDeadline = DateTime.UtcNow.AddDays(7),
            Status = ClassStatus.Open,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(11, 0)
        };
        db.Classes.Add(cls);
        await db.SaveChangesAsync();

        return (db, cls.Id, student.Id);
    }

    private static EnrollmentProcessor NewProcessor(AppDbContext db)
        => new(db, NullLogger<EnrollmentProcessor>.Instance);

    [Fact]
    public async Task Approves_And_Increments_Count_When_Seat_Available()
    {
        var (db, classId, studentId) = await SeedAsync(capacity: 2, enrolledCount: 0);
        var request = new EnrollmentRequest { ClassId = classId, StudentId = studentId };
        db.EnrollmentRequests.Add(request);
        await db.SaveChangesAsync();

        await NewProcessor(db).ProcessAsync(request.Id);

        var updated = await db.EnrollmentRequests.FirstAsync(r => r.Id == request.Id);
        var cls = await db.Classes.FirstAsync(c => c.Id == classId);
        Assert.Equal(EnrollmentRequestStatus.Approved, updated.Status);
        Assert.Equal(1, cls.EnrolledCount);
        Assert.Equal(1, await db.Enrollments.CountAsync());
    }

    [Fact]
    public async Task Rejects_When_Class_Full()
    {
        var (db, classId, studentId) = await SeedAsync(capacity: 1, enrolledCount: 1);
        var request = new EnrollmentRequest { ClassId = classId, StudentId = studentId };
        db.EnrollmentRequests.Add(request);
        await db.SaveChangesAsync();

        await NewProcessor(db).ProcessAsync(request.Id);

        var updated = await db.EnrollmentRequests.FirstAsync(r => r.Id == request.Id);
        Assert.Equal(EnrollmentRequestStatus.Rejected, updated.Status);
        Assert.Equal("Class is full.", updated.FailureReason);
        Assert.Equal(0, await db.Enrollments.CountAsync());
    }

    [Fact]
    public async Task Rejects_Duplicate_Enrollment()
    {
        var (db, classId, studentId) = await SeedAsync(capacity: 5, enrolledCount: 1);
        db.Enrollments.Add(new Enrollment { ClassId = classId, StudentId = studentId, Status = EnrollmentStatus.Active });
        await db.SaveChangesAsync();

        var request = new EnrollmentRequest { ClassId = classId, StudentId = studentId };
        db.EnrollmentRequests.Add(request);
        await db.SaveChangesAsync();

        await NewProcessor(db).ProcessAsync(request.Id);

        var updated = await db.EnrollmentRequests.FirstAsync(r => r.Id == request.Id);
        Assert.Equal(EnrollmentRequestStatus.Rejected, updated.Status);
        Assert.Equal("You are already enrolled in this class.", updated.FailureReason);
    }

    [Fact]
    public async Task Is_Idempotent_For_Already_Approved_Request()
    {
        var (db, classId, studentId) = await SeedAsync(capacity: 5, enrolledCount: 0);
        var request = new EnrollmentRequest
        {
            ClassId = classId, StudentId = studentId, Status = EnrollmentRequestStatus.Approved
        };
        db.EnrollmentRequests.Add(request);
        await db.SaveChangesAsync();

        await NewProcessor(db).ProcessAsync(request.Id);

        // No new enrollment created, count untouched.
        Assert.Equal(0, await db.Enrollments.CountAsync());
        var cls = await db.Classes.FirstAsync(c => c.Id == classId);
        Assert.Equal(0, cls.EnrolledCount);
    }
}
