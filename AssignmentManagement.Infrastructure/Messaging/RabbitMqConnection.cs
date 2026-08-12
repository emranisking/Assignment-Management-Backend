using RabbitMQ.Client;

namespace AssignmentManagement.Infrastructure.Messaging;

/// <summary>Owns a single recovering RabbitMQ connection shared by the publisher and consumer.</summary>
public class RabbitMqConnection : IDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly object _lock = new();
    private IConnection? _connection;

    public RabbitMqConnection(RabbitMqOptions options) => _options = options;

    public IConnection GetConnection()
    {
        if (_connection is { IsOpen: true }) return _connection;

        lock (_lock)
        {
            if (_connection is { IsOpen: true }) return _connection;

            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                UserName = _options.Username,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection("assignment-management");
            return _connection;
        }
    }

    public IModel CreateChannel() => GetConnection().CreateModel();

    public void Dispose()
    {
        try { _connection?.Dispose(); } catch { /* ignore */ }
    }
}
