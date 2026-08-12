using AssignmentManagement.Common.Models;
using AssignmentManagement.Domain.Enums;

namespace AssignmentManagement.Domain.Entities;

/// <summary>
/// A concrete offering of a Course with its own teacher, schedule and capacity.
/// Enrollment capacity lives here (not on Course).
/// </summary>
public class Class : BaseEntity
{
    public long CourseId { get; set; }
    public Course? Course { get; set; }

    /// <summary>Assigned teacher. Null until an Admin assigns one.</summary>
    public long? TeacherId { get; set; }
    public User? Teacher { get; set; }

    public string Name { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public int Capacity { get; set; }
    /// <summary>Denormalized counter maintained inside the locked enrollment transaction.</summary>
    public int EnrolledCount { get; set; }

    public DateTime EnrollmentDeadline { get; set; }
    public ClassStatus Status { get; set; } = ClassStatus.Open;

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}
