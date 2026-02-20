using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Test.Contracts.Constants;

namespace Test.Infrastructure.RabbitMq;

public class RabbitMqTopologySetup
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqTopologySetup> _logger;

    public RabbitMqTopologySetup(
        RabbitMqConnectionFactory connectionFactory,
        ILogger<RabbitMqTopologySetup> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public void Configure()
    {
        using var channel = _connectionFactory.CreateChannel();

        _logger.LogInformation("Setting up RabbitMQ topology...");

        // Declare Dead Letter Exchange
        channel.ExchangeDeclare(
            exchange: RabbitMqConstants.DeadLetterExchange,
            type: ExchangeType.Topic,
            durable: true);

        // Declare Events Exchange
        channel.ExchangeDeclare(
            exchange: RabbitMqConstants.EventsExchange,
            type: ExchangeType.Topic,
            durable: true);

        // Declare Commands Exchange
        channel.ExchangeDeclare(
            exchange: RabbitMqConstants.CommandsExchange,
            type: ExchangeType.Topic,
            durable: true);

        // Dead Letter Queues
        channel.QueueDeclare(
            queue: RabbitMqConstants.WorkerDlq,
            durable: true,
            exclusive: false,
            autoDelete: false);

        channel.QueueBind(
            queue: RabbitMqConstants.WorkerDlq,
            exchange: RabbitMqConstants.DeadLetterExchange,
            routingKey: "worker.#");

        channel.QueueDeclare(
            queue: RabbitMqConstants.MonitorDlq,
            durable: true,
            exclusive: false,
            autoDelete: false);

        channel.QueueBind(
            queue: RabbitMqConstants.MonitorDlq,
            exchange: RabbitMqConstants.DeadLetterExchange,
            routingKey: "monitor.#");

        // Retry Queue with TTL
        var retryArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", RabbitMqConstants.EventsExchange },
            { "x-message-ttl", RabbitMqConstants.RetryDelayMs }
        };

        channel.QueueDeclare(
            queue: RabbitMqConstants.RetryQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: retryArgs);

        // Worker Queue (consumes test.created)
        var workerArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", RabbitMqConstants.DeadLetterExchange },
            { "x-dead-letter-routing-key", "worker.dead" }
        };

        channel.QueueDeclare(
            queue: RabbitMqConstants.WorkerQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: workerArgs);

        channel.QueueBind(
            queue: RabbitMqConstants.WorkerQueue,
            exchange: RabbitMqConstants.EventsExchange,
            routingKey: RabbitMqConstants.TestCreated);

        // Monitor Queue (consumes all test events)
        var monitorArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", RabbitMqConstants.DeadLetterExchange },
            { "x-dead-letter-routing-key", "monitor.dead" }
        };

        channel.QueueDeclare(
            queue: RabbitMqConstants.MonitorQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: monitorArgs);

        channel.QueueBind(
            queue: RabbitMqConstants.MonitorQueue,
            exchange: RabbitMqConstants.EventsExchange,
            routingKey: "test.*");

        // Cancel Queue (worker listens for cancel commands)
        channel.QueueDeclare(
            queue: RabbitMqConstants.CancelQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);

        channel.QueueBind(
            queue: RabbitMqConstants.CancelQueue,
            exchange: RabbitMqConstants.CommandsExchange,
            routingKey: RabbitMqConstants.CancelTest);

        _logger.LogInformation("RabbitMQ topology configured successfully");
    }
}
