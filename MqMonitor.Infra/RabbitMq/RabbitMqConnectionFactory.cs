using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MqMonitor.Infra.Configuration;
using RabbitMQ.Client;

namespace MqMonitor.Infra.RabbitMq;

public class RabbitMqConnectionFactory : IDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqConnectionFactory> _logger;
    private IConnection? _connection;
    private readonly object _lock = new();

    public RabbitMqConnectionFactory(
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqConnectionFactory> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public IConnection GetConnection()
    {
        if (_connection is { IsOpen: true })
            return _connection;

        lock (_lock)
        {
            if (_connection is { IsOpen: true })
                return _connection;

            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost,
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _logger.LogInformation(
                "Connecting to RabbitMQ at {Host}:{Port}",
                _settings.HostName, _settings.Port);

            _connection = factory.CreateConnection();

            _logger.LogInformation("Connected to RabbitMQ successfully");

            return _connection;
        }
    }

    public IModel CreateChannel()
    {
        return GetConnection().CreateModel();
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
