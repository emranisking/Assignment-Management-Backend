using System.ComponentModel.DataAnnotations;
using AssignmentManagement.Domain.Enums;

namespace AssignmentManagement.Application.Classes.DTOs;

public class CreateClassRequest
{
    [Required]
    public long CourseId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public long? TeacherId { get; set; }

    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>Format HH:mm (24h).</summary>
    [Required]
    public string StartTime { get; set; } = "09:00";

    [Required]
    public string EndTime { get; set; } = "11:00";

    [Range(1, 1000)]
    public int Capacity { get; set; } = 40;

    [Required]
    public DateTime EnrollmentDeadline { get; set; }
}

public class UpdateClassRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    [Required]
    public string StartTime { get; set; } = "09:00";

    [Required]
    public string EndTime { get; set; } = "11:00";

    [Range(1, 1000)]
    public int Capacity { get; set; } = 40;

    [Required]
    public DateTime EnrollmentDeadline { get; set; }
}

public class AssignTeacherRequest
{
    [Required]
    public long TeacherId { get; set; }
}

public class UpdateClassStatusRequest
{
    [Required]
    public ClassStatus Status { get; set; }
}

public class ClassResponse
{
    public long Id { get; set; }
    public long CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int EnrolledCount { get; set; }
    public int AvailableSeats => Capacity - EnrolledCount;
    public DateTime EnrollmentDeadline { get; set; }
    public ClassStatus Status { get; set; }
}

public class ClassStudentResponse
{
    public long StudentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public EnrollmentStatus Status { get; set; }
    public DateTime EnrolledAt { get; set; }
}
