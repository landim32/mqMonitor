using System.Text;
using System.Text.Json;
using MqMonitor.Infra.Configuration;
using MqMonitor.Infra.Messaging.Contracts;
using MqMonitor.Infra.RabbitMq;
using MqMonitor.Worker.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MqMonitor.Worker.Handlers;

public class ProcessCreatedHandler : BackgroundService
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProcessCreatedHandler> _logger;
    private readonly string _workerName;
    private IModel? _channel;

    public ProcessCreatedHandler(
        RabbitMqConnectionFactory connectionFactory,
        IServiceScopeFactory scopeFactory,
        ILogger<ProcessCreatedHandler> logger)
    {
        _connectionFactory = connectionFactory;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _workerName = $"worker-{Environment.MachineName}-{Guid.NewGuid().ToString()[..8]}";
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _connectionFactory.CreateChannel();
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (_, ea) =>
        {
            var retryCount = GetRetryCount(ea.BasicProperties);

            try
            {
                var processEvent = JsonSerializer.Deserialize<ProcessEvent>(
                    Encoding.UTF8.GetString(ea.Body.ToArray()));

                if (processEvent == null)
                {
                    _logger.LogWarning("Received null process event, rejecting");
                    _channel.BasicReject(ea.DeliveryTag, requeue: false);
                    return;
                }

                _logger.LogInformation(
                    "Worker {Worker} received process.created for {ProcessId}",
                    _workerName, processEvent.ProcessId);

                using var scope = _scopeFactory.CreateScope();
                var executor = scope.ServiceProvider
                    .GetRequiredService<ProcessExecutorService>();

                await executor.ExecuteProcess(processEvent.ProcessId, _workerName);

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling process.created event");

                if (retryCount < RabbitMqConstants.MaxRetryCount)
                {
                    RetryMessage(_channel, ea, retryCount);
                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                else
                {
                    _channel.BasicReject(ea.DeliveryTag, requeue: false);
                }
            }
        };

        _channel.BasicConsume(
            queue: RabbitMqConstants.WorkerQueue,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation(
            "Worker {Worker} started, listening on {Queue}",
            _workerName, RabbitMqConstants.WorkerQueue);

        return Task.CompletedTask;
    }

    private static int GetRetryCount(IBasicProperties properties)
    {
        if (properties.Headers != null &&
            properties.Headers.TryGetValue("x-retry-count", out var value))
        {
            return Convert.ToInt32(value);
        }
        return 0;
    }

    private void RetryMessage(IModel channel, BasicDeliverEventArgs ea, int currentRetryCount)
    {
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.Headers = new Dictionary<string, object>
        {
            { "x-retry-count", currentRetryCount + 1 }
        };

        channel.BasicPublish(
            exchange: "",
            routingKey: RabbitMqConstants.RetryQueue,
            basicProperties: properties,
            body: ea.Body);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}
