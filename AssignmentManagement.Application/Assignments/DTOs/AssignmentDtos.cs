using System.ComponentModel.DataAnnotations;
using AssignmentManagement.Domain.Enums;

namespace AssignmentManagement.Application.Assignments.DTOs;

public class CreateAssignmentRequest
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public DateTime Deadline { get; set; }

    [Range(1, 1000)]
    public int MaxMarks { get; set; } = 100;
}

public class UpdateAssignmentRequest
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public DateTime Deadline { get; set; }

    [Range(1, 1000)]
    public int MaxMarks { get; set; } = 100;
}

public class AssignmentResponse
{
    public long Id { get; set; }
    public long ClassId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime Deadline { get; set; }
    public int MaxMarks { get; set; }
    public AssignmentStatus Status { get; set; }
    public bool ResultsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
}
