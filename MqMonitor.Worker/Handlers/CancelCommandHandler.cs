using System.Text;
using System.Text.Json;
using MqMonitor.Infra.Configuration;
using MqMonitor.Infra.Messaging.Contracts;
using MqMonitor.Infra.RabbitMq;
using MqMonitor.Worker.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MqMonitor.Worker.Handlers;

public class CancelCommandHandler : BackgroundService
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly CancellationTokenManager _cancellationManager;
    private readonly ILogger<CancelCommandHandler> _logger;
    private IModel? _channel;

    public CancelCommandHandler(
        RabbitMqConnectionFactory connectionFactory,
        CancellationTokenManager cancellationManager,
        ILogger<CancelCommandHandler> logger)
    {
        _connectionFactory = connectionFactory;
        _cancellationManager = cancellationManager;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _connectionFactory.CreateChannel();

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (_, ea) =>
        {
            try
            {
                var command = JsonSerializer.Deserialize<CancelProcessCommand>(
                    Encoding.UTF8.GetString(ea.Body.ToArray()));

                if (command == null)
                {
                    _channel.BasicReject(ea.DeliveryTag, requeue: false);
                    return;
                }

                _logger.LogInformation(
                    "Received cancel command {CommandId} for process {ProcessId}",
                    command.CommandId, command.ProcessId);

                _cancellationManager.TryCancel(command.ProcessId);

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling cancel command");
                _channel.BasicReject(ea.DeliveryTag, requeue: false);
            }

            await Task.CompletedTask;
        };

        _channel.BasicConsume(
            queue: RabbitMqConstants.CancelQueue,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("CancelCommandHandler started, listening on {Queue}",
            RabbitMqConstants.CancelQueue);

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}
