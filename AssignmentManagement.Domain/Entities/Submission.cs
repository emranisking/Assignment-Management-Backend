using AssignmentManagement.Common.Models;
using AssignmentManagement.Domain.Enums;

namespace AssignmentManagement.Domain.Entities;

/// <summary>
/// One submission per (Assignment, Student). Each upload adds a SubmissionVersion;
/// CurrentVersion points at the latest one. Grading fields live here.
/// </summary>
public class Submission : BaseEntity
{
    public long AssignmentId { get; set; }
    public Assignment? Assignment { get; set; }

    public long StudentId { get; set; }
    public User? Student { get; set; }

    public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;
    public int CurrentVersion { get; set; }

    public int? Marks { get; set; }
    public string? Feedback { get; set; }
    public DateTime? GradedAt { get; set; }
    public long? GradedById { get; set; }

    public ICollection<SubmissionVersion> Versions { get; set; } = new List<SubmissionVersion>();
}
