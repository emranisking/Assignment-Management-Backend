using AssignmentManagement.Common.Models;
using AssignmentManagement.Domain.Enums;

namespace AssignmentManagement.Domain.Entities;

/// <summary>A teacher applies to teach a course; an Admin approves or rejects.</summary>
public class TeacherCourseApplication : BaseEntity
{
    public long TeacherId { get; set; }
    public User? Teacher { get; set; }

    public long CourseId { get; set; }
    public Course? Course { get; set; }

    public CourseApplicationStatus Status { get; set; } = CourseApplicationStatus.Pending;
    public string? DecisionNote { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
