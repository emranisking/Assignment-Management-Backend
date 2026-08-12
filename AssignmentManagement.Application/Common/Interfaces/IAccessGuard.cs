using AssignmentManagement.Domain.Entities;

namespace AssignmentManagement.Application.Common.Interfaces;

/// <summary>
/// Resource-level authorization helpers (beyond JWT role checks):
/// "is THIS teacher assigned to THIS class", "is THIS student enrolled here".
/// </summary>
public interface IAccessGuard
{
    /// <summary>Loads the class and ensures the current user may manage it (owning teacher or admin).</summary>
    Task<Class> RequireManageableClassAsync(long classId, CancellationToken ct = default);

    /// <summary>Ensures the current student is actively enrolled in the class.</summary>
    Task RequireEnrolledAsync(long classId, CancellationToken ct = default);

    /// <summary>Loads an assignment (with its class) and ensures the current user may manage it.</summary>
    Task<Assignment> RequireManageableAssignmentAsync(long assignmentId, CancellationToken ct = default);
}
