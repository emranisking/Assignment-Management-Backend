namespace AssignmentManagement.Common.Exceptions;

public class UnauthorizedAppException : AppException
{
    public override int StatusCode => 401;
    public UnauthorizedAppException(string message = "Unauthorized.") : base(message) { }
}
