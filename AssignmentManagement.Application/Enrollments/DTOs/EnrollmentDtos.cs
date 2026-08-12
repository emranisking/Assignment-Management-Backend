using AssignmentManagement.Domain.Enums;

namespace AssignmentManagement.Application.Enrollments.DTOs;

public class EnrollmentRequestResponse
{
    public long RequestId { get; set; }
    public long ClassId { get; set; }
    public EnrollmentRequestStatus Status { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class EnrollmentResponse
{
    public long Id { get; set; }
    public long ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public EnrollmentStatus Status { get; set; }
    public DateTime EnrolledAt { get; set; }
}
