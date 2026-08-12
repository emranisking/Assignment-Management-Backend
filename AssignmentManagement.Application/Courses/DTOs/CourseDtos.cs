using System.ComponentModel.DataAnnotations;

namespace AssignmentManagement.Application.Courses.DTOs;

public class CreateCourseRequest
{
    [Required, MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(1, 12)]
    public int CreditHours { get; set; } = 3;
}

public class UpdateCourseRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(1, 12)]
    public int CreditHours { get; set; } = 3;
}

public class CourseResponse
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CreditHours { get; set; }
    public DateTime CreatedAt { get; set; }
}
