using AssignmentManagement.Common.Models;

namespace AssignmentManagement.Domain.Entities;

public class SubmissionVersion : BaseEntity
{
    public long SubmissionId { get; set; }
    public Submission? Submission { get; set; }

    public int VersionNumber { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
}
