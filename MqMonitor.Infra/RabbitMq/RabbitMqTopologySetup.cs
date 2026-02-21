using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MqMonitor.Infra.Configuration;
using RabbitMQ.Client;

namespace MqMonitor.Infra.RabbitMq;

public class RabbitMqTopologySetup
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly PipelineSettings _pipelineSettings;
    private readonly ILogger<RabbitMqTopologySetup> _logger;

    public RabbitMqTopologySetup(
        RabbitMqConnectionFactory connectionFactory,
        IOptions<PipelineSettings> pipelineSettings,
        ILogger<RabbitMqTopologySetup> logger)
    {
        _connectionFactory = connectionFactory;
        _pipelineSettings = pipelineSettings.Value;
        _logger = logger;
    }

    public void Configure()
    {
        using var channel = _connectionFactory.CreateChannel();

        _logger.LogInformation("Setting up RabbitMQ topology...");

        // === Exchanges ===

        // Dead Letter Exchange
        channel.ExchangeDeclare(
            exchange: RabbitMqConstants.DeadLetterExchange,
            type: ExchangeType.Topic,
            durable: true);

        // Events Exchange (process lifecycle events)
        channel.ExchangeDeclare(
            exchange: RabbitMqConstants.EventsExchange,
            type: ExchangeType.Topic,
            durable: true);

        // Commands Exchange
        channel.ExchangeDeclare(
            exchange: RabbitMqConstants.CommandsExchange,
            type: ExchangeType.Topic,
            durable: true);

        // Pipeline Exchange (stage routing)
        channel.ExchangeDeclare(
            exchange: _pipelineSettings.PipelineExchange,
            type: ExchangeType.Topic,
            durable: true);

        // === Dead Letter Queues ===

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

        // === Retry Queue with TTL ===

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

        // === Worker Queue (consumes process.created) ===

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
            routingKey: RabbitMqConstants.ProcessCreated);

        // === Monitor Queue (consumes ALL process events including multi-dot routing keys) ===

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

        // Changed from process.* to process.# to capture multi-dot keys like process.stage.started
        channel.QueueBind(
            queue: RabbitMqConstants.MonitorQueue,
            exchange: RabbitMqConstants.EventsExchange,
            routingKey: "process.#");

        // === Cancel Queue ===

        channel.QueueDeclare(
            queue: RabbitMqConstants.CancelQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);

        channel.QueueBind(
            queue: RabbitMqConstants.CancelQueue,
            exchange: RabbitMqConstants.CommandsExchange,
            routingKey: RabbitMqConstants.CancelProcess);

        // === Compensation Queue ===

        channel.QueueDeclare(
            queue: RabbitMqConstants.CompensationQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);

        channel.QueueBind(
            queue: RabbitMqConstants.CompensationQueue,
            exchange: RabbitMqConstants.EventsExchange,
            routingKey: RabbitMqConstants.ProcessCompensating);

        // === Dynamic Pipeline Stage Queues ===

        foreach (var stage in _pipelineSettings.Stages)
        {
            var stageArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", RabbitMqConstants.DeadLetterExchange },
                { "x-dead-letter-routing-key", $"pipeline.{stage.Name}.dead" },
                { "x-max-priority", stage.MaxPriority }
            };

            channel.QueueDeclare(
                queue: stage.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: stageArgs);

            channel.QueueBind(
                queue: stage.QueueName,
                exchange: _pipelineSettings.PipelineExchange,
                routingKey: stage.RoutingKey);

            _logger.LogInformation(
                "Configured pipeline stage '{StageName}' → queue '{QueueName}' (routing: {RoutingKey}, maxPriority: {MaxPriority})",
                stage.DisplayName, stage.QueueName, stage.RoutingKey, stage.MaxPriority);

            // Stage-specific DLQ if configured
            if (!string.IsNullOrEmpty(stage.DlqName))
            {
                channel.QueueDeclare(
                    queue: stage.DlqName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                channel.QueueBind(
                    queue: stage.DlqName,
                    exchange: RabbitMqConstants.DeadLetterExchange,
                    routingKey: $"pipeline.{stage.Name}.#");
            }

            // Per-stage retry queue with TTL → routes back to stage queue via pipeline exchange
            var retryQueueName = $"{stage.QueueName}.retry";
            var stageRetryArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", _pipelineSettings.PipelineExchange },
                { "x-dead-letter-routing-key", stage.RoutingKey },
                { "x-message-ttl", stage.RetryDelayMs }
            };

            channel.QueueDeclare(
                queue: retryQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: stageRetryArgs);

            _logger.LogInformation(
                "Configured retry queue '{RetryQueue}' for stage '{Stage}' (TTL: {Ttl}ms)",
                retryQueueName, stage.Name, stage.RetryDelayMs);
        }

        _logger.LogInformation(
            "RabbitMQ topology configured successfully with {StageCount} pipeline stages",
            _pipelineSettings.Stages.Count);
    }
}
