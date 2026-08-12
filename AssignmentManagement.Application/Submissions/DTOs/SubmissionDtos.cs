using System.ComponentModel.DataAnnotations;
using AssignmentManagement.Domain.Enums;

namespace AssignmentManagement.Application.Submissions.DTOs;

/// <summary>Transport-agnostic file upload (the controller adapts IFormFile into this).</summary>
public class SubmissionFileUpload
{
    public Stream Content { get; set; } = Stream.Null;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Length { get; set; }
}

public class GradeSubmissionRequest
{
    [Range(0, 1000)]
    public int Marks { get; set; }

    [MaxLength(2000)]
    public string? Feedback { get; set; }
}

public class SubmissionResponse
{
    public long Id { get; set; }
    public long AssignmentId { get; set; }
    public long StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public SubmissionStatus Status { get; set; }
    public int CurrentVersion { get; set; }
    public int? Marks { get; set; }
    public string? Feedback { get; set; }
    public DateTime? GradedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SubmissionVersionResponse
{
    public long Id { get; set; }
    public int VersionNumber { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
}

public class DownloadResult
{
    public Stream Content { get; set; } = Stream.Null;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
}
