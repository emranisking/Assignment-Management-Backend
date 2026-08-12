using AssignmentManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace AssignmentManagement.Application.Common.Interfaces;

/// <summary>
/// Persistence abstraction so the Application layer never depends on the concrete DbContext.
/// Exposes the Database facade for the pessimistic-locking enrollment transaction.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Course> Courses { get; }
    DbSet<Class> Classes { get; }
    DbSet<TeacherCourseApplication> TeacherCourseApplications { get; }
    DbSet<EnrollmentRequest> EnrollmentRequests { get; }
    DbSet<Enrollment> Enrollments { get; }
    DbSet<Assignment> Assignments { get; }
    DbSet<Submission> Submissions { get; }
    DbSet<SubmissionVersion> SubmissionVersions { get; }
    DbSet<ResubmissionRequest> ResubmissionRequests { get; }

    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
