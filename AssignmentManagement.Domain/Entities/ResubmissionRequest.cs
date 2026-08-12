using AssignmentManagement.Common.Models;
using AssignmentManagement.Domain.Enums;

namespace AssignmentManagement.Domain.Entities;

/// <summary>Student asks to resubmit a graded assignment; the owning teacher decides.</summary>
public class ResubmissionRequest : BaseEntity
{
    public long SubmissionId { get; set; }
    public Submission? Submission { get; set; }

    public long StudentId { get; set; }
    public User? Student { get; set; }

    public string Reason { get; set; } = string.Empty;
    public ResubmissionRequestStatus Status { get; set; } = ResubmissionRequestStatus.Pending;
    public string? DecisionNote { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
