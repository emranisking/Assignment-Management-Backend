using AssignmentManagement.Domain.Enums;

namespace AssignmentManagement.Application.Results.DTOs;

public class ResultResponse
{
    public long AssignmentId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public long ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public int MaxMarks { get; set; }
    public int? Marks { get; set; }
    public string? Feedback { get; set; }
    public SubmissionStatus? SubmissionStatus { get; set; }
    public DateTime? GradedAt { get; set; }
}

public class ClassResultRowResponse
{
    public long StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public long AssignmentId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public int MaxMarks { get; set; }
    public int? Marks { get; set; }
    public SubmissionStatus? Status { get; set; }
}
