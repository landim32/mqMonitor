using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Test.Contracts.Constants;
using Test.Contracts.Events;
using Test.Infrastructure.RabbitMq;
using Test.Monitor.Services;

namespace Test.Monitor.Consumers;

public class TestEventConsumer : BackgroundService
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TestEventConsumer> _logger;
    private IModel? _channel;

    public TestEventConsumer(
        RabbitMqConnectionFactory connectionFactory,
        IServiceScopeFactory scopeFactory,
        ILogger<TestEventConsumer> logger)
    {
        _connectionFactory = connectionFactory;
        _scopeFactory = scopeFactory;
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
                var testEvent = JsonSerializer.Deserialize<TestEvent>(
                    Encoding.UTF8.GetString(body));

                if (testEvent == null)
                {
                    _logger.LogWarning("Received null event, rejecting");
                    _channel.BasicReject(ea.DeliveryTag, requeue: false);
                    return;
                }

                _logger.LogInformation(
                    "Received event {EventId} ({RoutingKey}) for test {TestId}",
                    testEvent.EventId, routingKey, testEvent.TestId);

                using var scope = _scopeFactory.CreateScope();
                var projectionService = scope.ServiceProvider
                    .GetRequiredService<EventProjectionService>();

                await projectionService.ProjectEvent(testEvent, routingKey);

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing event with routing key {RoutingKey}",
                    routingKey);

                if (retryCount < RabbitMqConstants.MaxRetryCount)
                {
                    // Send to retry queue
                    RetryMessage(_channel, ea, retryCount);
                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                else
                {
                    // Exceeded max retries, send to DLQ
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

        _logger.LogInformation("TestEventConsumer started, listening on {Queue}",
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
