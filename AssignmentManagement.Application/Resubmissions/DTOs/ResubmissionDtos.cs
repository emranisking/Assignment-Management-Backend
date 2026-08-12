using System.ComponentModel.DataAnnotations;
using AssignmentManagement.Domain.Enums;

namespace AssignmentManagement.Application.Resubmissions.DTOs;

public class CreateResubmissionRequest
{
    [Required, MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;
}

public class ResubmissionDecisionRequest
{
    [MaxLength(500)]
    public string? Note { get; set; }
}

public class ResubmissionResponse
{
    public long Id { get; set; }
    public long SubmissionId { get; set; }
    public long StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public long AssignmentId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public ResubmissionRequestStatus Status { get; set; }
    public string? DecisionNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
