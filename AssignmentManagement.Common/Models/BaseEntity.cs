namespace AssignmentManagement.Common.Models;

/// <summary>
/// Base type for every persisted entity. Keeps Id and audit timestamps in one place.
/// </summary>
public abstract class BaseEntity
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
