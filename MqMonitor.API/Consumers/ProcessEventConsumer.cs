using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using MqMonitor.API.Hubs;
using MqMonitor.Domain.Services.Interfaces;
using MqMonitor.DTO;
using MqMonitor.Infra.Configuration;
using MqMonitor.Infra.Messaging.Contracts;
using MqMonitor.Infra.RabbitMq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MqMonitor.API.Consumers;

public class ProcessEventConsumer : BackgroundService
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<MonitorHub> _hubContext;
    private readonly ILogger<ProcessEventConsumer> _logger;
    private IModel? _channel;

    public ProcessEventConsumer(
        RabbitMqConnectionFactory connectionFactory,
        IServiceScopeFactory scopeFactory,
        IHubContext<MonitorHub> hubContext,
        ILogger<ProcessEventConsumer> logger)
    {
        _connectionFactory = connectionFactory;
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _connectionFactory.CreateChannel();
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (_, ea) =>
        {
            var routingKey = ea.RoutingKey;
            var body = ea.Body.ToArray();
            var retryCount = GetRetryCount(ea.BasicProperties);

            try
            {
                var processEvent = JsonSerializer.Deserialize<ProcessEvent>(
                    Encoding.UTF8.GetString(body));

                if (processEvent == null)
                {
                    _logger.LogWarning("Received null event, rejecting");
                    _channel.BasicReject(ea.DeliveryTag, requeue: false);
                    return;
                }

                _logger.LogInformation(
                    "Received event {EventId} ({RoutingKey}) for process {ProcessId}",
                    processEvent.EventId, routingKey, processEvent.ProcessId);

                using var scope = _scopeFactory.CreateScope();
                var projectionService = scope.ServiceProvider
                    .GetRequiredService<IEventProjectionService>();

                var payload = Encoding.UTF8.GetString(body);
                await projectionService.ProjectEventAsync(
                    processEvent.EventId, processEvent.ProcessId, processEvent.Status,
                    processEvent.Timestamp, processEvent.Worker, processEvent.ErrorMessage,
                    routingKey, payload,
                    processEvent.Message, processEvent.CurrentStage, processEvent.Priority);

                // Push real-time update via SignalR
                var dto = new ProcessExecutionInfo
                {
                    ProcessId = processEvent.ProcessId,
                    Status = processEvent.Status,
                    Worker = processEvent.Worker,
                    CurrentStage = processEvent.CurrentStage,
                    Message = processEvent.Message,
                    Priority = processEvent.Priority,
                    ErrorMessage = processEvent.ErrorMessage
                };

                await _hubContext.Clients.Group($"process-{processEvent.ProcessId}")
                    .SendAsync("ProcessUpdated", dto);
                await _hubContext.Clients.Group("all")
                    .SendAsync("ProcessUpdated", dto);

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing event with routing key {RoutingKey}",
                    routingKey);

                if (retryCount < RabbitMqConstants.MaxRetryCount)
                {
                    RetryMessage(_channel, ea, retryCount);
                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                else
                {
                    _logger.LogError(
                        "Max retries ({MaxRetries}) exceeded, sending to DLQ",
                        RabbitMqConstants.MaxRetryCount);
                    _channel.BasicReject(ea.DeliveryTag, requeue: false);
                }
            }
        };

        _channel.BasicConsume(
            queue: RabbitMqConstants.MonitorQueue,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("ProcessEventConsumer started, listening on {Queue}",
            RabbitMqConstants.MonitorQueue);

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
            { "x-retry-count", currentRetryCount + 1 },
            { "x-original-routing-key", ea.RoutingKey }
        };

        channel.BasicPublish(
            exchange: "",
            routingKey: RabbitMqConstants.RetryQueue,
            basicProperties: properties,
            body: ea.Body);

        _logger.LogWarning(
            "Message sent to retry queue (attempt {Attempt}/{Max})",
            currentRetryCount + 1, RabbitMqConstants.MaxRetryCount);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}
