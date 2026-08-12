using AssignmentManagement.Common.Models;
using AssignmentManagement.Domain.Enums;

namespace AssignmentManagement.Domain.Entities;

public class Assignment : BaseEntity
{
    public long ClassId { get; set; }
    public Class? Class { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime Deadline { get; set; }
    public int MaxMarks { get; set; } = 100;

    public AssignmentStatus Status { get; set; } = AssignmentStatus.Draft;
    /// <summary>Students only see marks/feedback once the teacher publishes results.</summary>
    public bool ResultsPublished { get; set; }

    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
