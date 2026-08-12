using System.Text;
using System.Text.Json;
using AssignmentManagement.Application.Common.Interfaces;
using RabbitMQ.Client;

namespace AssignmentManagement.Infrastructure.Messaging;

public class RabbitMqPublisher : IMessagePublisher
{
    private readonly RabbitMqConnection _connection;

    public RabbitMqPublisher(RabbitMqConnection connection) => _connection = connection;

    public void Publish<T>(T message, string queueName)
    {
        using var channel = _connection.CreateChannel();
        channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var props = channel.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";

        channel.BasicPublish(exchange: string.Empty, routingKey: queueName, basicProperties: props, body: body);
    }
}
