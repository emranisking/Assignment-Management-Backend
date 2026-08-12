namespace AssignmentManagement.Common.Exceptions;

public class NotFoundException : AppException
{
    public override int StatusCode => 404;
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string entity, object key)
        : base($"{entity} with id '{key}' was not found.") { }
}
