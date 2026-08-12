using System.ComponentModel.DataAnnotations;
using AssignmentManagement.Domain.Enums;

namespace AssignmentManagement.Application.TeacherApplications.DTOs;

public class CreateTeacherApplicationRequest
{
    [Required]
    public long CourseId { get; set; }
}

public class DecisionRequest
{
    [MaxLength(500)]
    public string? Note { get; set; }
}

public class TeacherApplicationResponse
{
    public long Id { get; set; }
    public long TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public long CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public CourseApplicationStatus Status { get; set; }
    public string? DecisionNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
