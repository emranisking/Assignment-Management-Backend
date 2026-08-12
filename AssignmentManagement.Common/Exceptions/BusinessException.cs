namespace AssignmentManagement.Common.Exceptions;

/// <summary>
/// A business rule was violated (e.g. class is full, deadline passed). Maps to 409 by default.
/// </summary>
public class BusinessException : AppException
{
    private readonly int _statusCode;
    public override int StatusCode => _statusCode;
    public BusinessException(string message, int statusCode = 409) : base(message)
        => _statusCode = statusCode;
}
