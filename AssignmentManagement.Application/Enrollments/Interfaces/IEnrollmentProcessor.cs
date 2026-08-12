namespace AssignmentManagement.Application.Enrollments.Interfaces;

/// <summary>
/// Runs the authoritative enrollment transaction (pessimistic lock on the Class row).
/// Idempotent: safe to run more than once for the same request id (RabbitMQ may redeliver).
/// </summary>
public interface IEnrollmentProcessor
{
    Task ProcessAsync(long requestId, CancellationToken ct = default);
}
