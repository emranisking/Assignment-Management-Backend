namespace AssignmentManagement.Common.Exceptions;

/// <summary>
/// Authenticated but not allowed to touch this specific resource (resource authorization).
/// </summary>
public class ForbiddenException : AppException
{
    public override int StatusCode => 403;
    public ForbiddenException(string message = "You are not allowed to access this resource.")
        : base(message) { }
}
