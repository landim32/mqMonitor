using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MqMonitor.Domain.Messaging.Interfaces;
using MqMonitor.Infra.Configuration;
using RabbitMQ.Client;

namespace MqMonitor.Infra.RabbitMq;

public class RabbitMqPublisher : IMessagePublisher
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly PipelineSettings _pipelineSettings;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(
        RabbitMqConnectionFactory connectionFactory,
        IOptions<PipelineSettings> pipelineSettings,
        ILogger<RabbitMqPublisher> logger)
    {
        _connectionFactory = connectionFactory;
        _pipelineSettings = pipelineSettings.Value;
        _logger = logger;
    }

    public void PublishEvent(object eventData, string routingKey)
    {
        PublishToExchange(RabbitMqConstants.EventsExchange, eventData, routingKey, priority: 0);
    }

    public void PublishEvent(object eventData, string routingKey, byte priority)
    {
        PublishToExchange(RabbitMqConstants.EventsExchange, eventData, routingKey, priority);
    }

    public void PublishToPipeline(object eventData, string routingKey, byte priority = 0)
    {
        PublishToExchange(_pipelineSettings.PipelineExchange, eventData, routingKey, priority);

        _logger.LogInformation(
            "Published to pipeline exchange with routing key {RoutingKey} and priority {Priority}",
            routingKey, priority);
    }

    public void PublishCommand(object command, string exchange, string routingKey)
    {
        PublishToExchange(exchange, command, routingKey, priority: 0);
    }

    private void PublishToExchange(string exchange, object data, string routingKey, byte priority)
    {
        using var channel = _connectionFactory.CreateChannel();

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data));

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        properties.ContentType = "application/json";

        if (priority > 0)
            properties.Priority = priority;

        channel.BasicPublish(
            exchange: exchange,
            routingKey: routingKey,
            basicProperties: properties,
            body: body);

        _logger.LogInformation(
            "Published to {Exchange} with routing key {RoutingKey}",
            exchange, routingKey);
    }
}
