using AssignmentManagement.Domain.Enums;

namespace AssignmentManagement.Application.Common.Interfaces;

/// <summary>Reads the authenticated principal (from the JWT) for the current request.</summary>
public interface ICurrentUser
{
    long? UserId { get; }
    string? Email { get; }
    UserRole? Role { get; }
    bool IsAuthenticated { get; }

    /// <summary>Returns the user id or throws Unauthorized if there is no authenticated user.</summary>
    long RequireUserId();
}
