using AssignmentManagement.Common.Models;
using AssignmentManagement.Domain.Enums;

namespace AssignmentManagement.Domain.Entities;

/// <summary>Authoritative enrollment record. UNIQUE(StudentId, ClassId) enforces no duplicates.</summary>
public class Enrollment : BaseEntity
{
    public long StudentId { get; set; }
    public User? Student { get; set; }

    public long ClassId { get; set; }
    public Class? Class { get; set; }

    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
}
