namespace AssignmentManagement.Common.Exceptions;

/// <summary>
/// Base class for all expected/handled application exceptions.
/// The global exception middleware maps StatusCode to the HTTP response.
/// </summary>
public abstract class AppException : Exception
{
    public abstract int StatusCode { get; }
    public IEnumerable<string>? Errors { get; }

    protected AppException(string message, IEnumerable<string>? errors = null) : base(message)
    {
        Errors = errors;
    }
}
