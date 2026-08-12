using AssignmentManagement.Application.Common.Interfaces;

namespace AssignmentManagement.Infrastructure.Messaging;

/// <summary>Used when RabbitMQ is disabled so IMessagePublisher still resolves (sync enrollment path).</summary>
public class NoOpMessagePublisher : IMessagePublisher
{
    public void Publish<T>(T message, string queueName) { /* intentionally no-op */ }
}
