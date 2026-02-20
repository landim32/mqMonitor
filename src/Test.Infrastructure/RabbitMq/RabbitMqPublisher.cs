using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Test.Contracts.Commands;
using Test.Contracts.Constants;
using Test.Contracts.Events;

namespace Test.Infrastructure.RabbitMq;

public class RabbitMqPublisher
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(
        RabbitMqConnectionFactory connectionFactory,
        ILogger<RabbitMqPublisher> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public void PublishEvent(TestEvent testEvent, string routingKey)
    {
        using var channel = _connectionFactory.CreateChannel();

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(testEvent));

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = testEvent.EventId;
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        properties.ContentType = "application/json";

        channel.BasicPublish(
            exchange: RabbitMqConstants.EventsExchange,
            routingKey: routingKey,
            basicProperties: properties,
            body: body);

        _logger.LogInformation(
            "Published event {EventId} with routing key {RoutingKey} for test {TestId}",
            testEvent.EventId, routingKey, testEvent.TestId);
    }

    public void PublishCancelCommand(CancelTestCommand command)
    {
        using var channel = _connectionFactory.CreateChannel();

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(command));

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = command.CommandId;
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        properties.ContentType = "application/json";

        channel.BasicPublish(
            exchange: RabbitMqConstants.CommandsExchange,
            routingKey: RabbitMqConstants.CancelTest,
            basicProperties: properties,
            body: body);

        _logger.LogInformation(
            "Published cancel command {CommandId} for test {TestId}",
            command.CommandId, command.TestId);
    }
}
