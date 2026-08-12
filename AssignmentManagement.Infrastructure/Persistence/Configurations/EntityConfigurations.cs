using AssignmentManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssignmentManagement.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(150);
        b.Property(x => x.Email).IsRequired().HasMaxLength(200);
        b.Property(x => x.PasswordHash).IsRequired();
        b.Property(x => x.Role).HasConversion<int>();
        b.HasIndex(x => x.Email).IsUnique();
    }
}

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> b)
    {
        b.ToTable("Courses");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).IsRequired().HasMaxLength(20);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public class ClassConfiguration : IEntityTypeConfiguration<Class>
{
    public void Configure(EntityTypeBuilder<Class> b)
    {
        b.ToTable("Classes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(100);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.DayOfWeek).HasConversion<int>();

        b.HasOne(x => x.Course)
            .WithMany(c => c.Classes)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Teacher)
            .WithMany()
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(x => x.CourseId);
        b.HasIndex(x => x.TeacherId);
    }
}

public class TeacherCourseApplicationConfiguration : IEntityTypeConfiguration<TeacherCourseApplication>
{
    public void Configure(EntityTypeBuilder<TeacherCourseApplication> b)
    {
        b.ToTable("TeacherCourseApplications");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.DecisionNote).HasMaxLength(500);

        b.HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.TeacherId, x.CourseId });
    }
}

public class EnrollmentRequestConfiguration : IEntityTypeConfiguration<EnrollmentRequest>
{
    public void Configure(EntityTypeBuilder<EnrollmentRequest> b)
    {
        b.ToTable("EnrollmentRequests");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.FailureReason).HasMaxLength(500);

        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Class).WithMany().HasForeignKey(x => x.ClassId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.StudentId, x.ClassId });
    }
}

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> b)
    {
        b.ToTable("Enrollments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasConversion<int>();

        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Class)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(x => x.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        // The critical invariant: a student can enroll in a given class only once.
        b.HasIndex(x => new { x.StudentId, x.ClassId }).IsUnique();
    }
}

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> b)
    {
        b.ToTable("Assignments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Status).HasConversion<int>();

        b.HasOne(x => x.Class)
            .WithMany(c => c.Assignments)
            .HasForeignKey(x => x.ClassId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.ClassId);
    }
}

public class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> b)
    {
        b.ToTable("Submissions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Feedback).HasMaxLength(2000);

        b.HasOne(x => x.Assignment)
            .WithMany(a => a.Submissions)
            .HasForeignKey(x => x.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.AssignmentId, x.StudentId }).IsUnique();
    }
}

public class SubmissionVersionConfiguration : IEntityTypeConfiguration<SubmissionVersion>
{
    public void Configure(EntityTypeBuilder<SubmissionVersion> b)
    {
        b.ToTable("SubmissionVersions");
        b.HasKey(x => x.Id);
        b.Property(x => x.FileName).IsRequired().HasMaxLength(300);
        b.Property(x => x.FilePath).IsRequired().HasMaxLength(500);
        b.Property(x => x.ContentType).HasMaxLength(150);

        b.HasOne(x => x.Submission)
            .WithMany(s => s.Versions)
            .HasForeignKey(x => x.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.SubmissionId, x.VersionNumber }).IsUnique();
    }
}

public class ResubmissionRequestConfiguration : IEntityTypeConfiguration<ResubmissionRequest>
{
    public void Configure(EntityTypeBuilder<ResubmissionRequest> b)
    {
        b.ToTable("ResubmissionRequests");
        b.HasKey(x => x.Id);
        b.Property(x => x.Reason).IsRequired().HasMaxLength(1000);
        b.Property(x => x.DecisionNote).HasMaxLength(500);
        b.Property(x => x.Status).HasConversion<int>();

        b.HasOne(x => x.Submission).WithMany().HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.SubmissionId);
    }
}
