namespace AssignmentManagement.Application.Enrollments.Messages;

/// <summary>
/// Deliberately small RabbitMQ payload. The worker re-reads authoritative data from PostgreSQL;
/// the database is the source of truth, not the message.
/// </summary>
public class EnrollmentMessage
{
    public long RequestId { get; set; }
    public long StudentId { get; set; }
    public long ClassId { get; set; }
}

public class EnrollmentOptions
{
    /// <summary>When true, requests are published to RabbitMQ and processed by the background worker.
    /// When false (e.g. local run without a broker), the request is processed synchronously.</summary>
    public bool UseAsyncProcessing { get; set; } = true;
    public string QueueName { get; set; } = "enrollment-requests";
}
