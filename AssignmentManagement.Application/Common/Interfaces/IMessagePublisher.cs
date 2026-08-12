namespace AssignmentManagement.Application.Common.Interfaces;

public interface IMessagePublisher
{
    void Publish<T>(T message, string queueName);
}
