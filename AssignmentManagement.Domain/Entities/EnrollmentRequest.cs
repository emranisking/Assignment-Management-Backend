using AssignmentManagement.Common.Models;
using AssignmentManagement.Domain.Enums;

namespace AssignmentManagement.Domain.Entities;

/// <summary>
/// The asynchronous "please enroll me" record. Created immediately by the API,
/// then resolved to Approved/Rejected by the enrollment worker.
/// </summary>
public class EnrollmentRequest : BaseEntity
{
    public long StudentId { get; set; }
    public User? Student { get; set; }

    public long ClassId { get; set; }
    public Class? Class { get; set; }

    public EnrollmentRequestStatus Status { get; set; } = EnrollmentRequestStatus.Pending;
    public string? FailureReason { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
