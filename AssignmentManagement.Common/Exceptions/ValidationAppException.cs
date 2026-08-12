namespace AssignmentManagement.Common.Exceptions;

public class ValidationAppException : AppException
{
    public override int StatusCode => 400;
    public ValidationAppException(string message, IEnumerable<string>? errors = null)
        : base(message, errors) { }
}
