namespace AssignmentManagement.Common.Models;

public class ErrorResponse
{
    public bool Success => false;
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public IEnumerable<string>? Errors { get; set; }
    public string? TraceId { get; set; }
}
