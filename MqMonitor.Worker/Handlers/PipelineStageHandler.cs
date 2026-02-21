using System.Text;
using System.Text.Json;
using MqMonitor.Domain.Enums;
using MqMonitor.Domain.Messaging.Interfaces;
using MqMonitor.Infra.Configuration;
using MqMonitor.Infra.Messaging.Contracts;
using MqMonitor.Infra.RabbitMq;
using MqMonitor.Worker.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MqMonitor.Worker.Handlers;

public class PipelineStageHandler : BackgroundService
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMessagePublisher _publisher;
    private readonly StageDefinition _stage;
    private readonly ILogger<PipelineStageHandler> _logger;
    private readonly string _workerName;
    private IModel? _channel;

    public PipelineStageHandler(
        RabbitMqConnectionFactory connectionFactory,
        IServiceScopeFactory scopeFactory,
        IMessagePublisher publisher,
        StageDefinition stage,
        ILogger<PipelineStageHandler> logger)
    {
        _connectionFactory = connectionFactory;
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _stage = stage;
        _logger = logger;
        _workerName = $"worker-{stage.Name}-{Environment.MachineName}-{Guid.NewGuid().ToString()[..8]}";
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _connectionFactory.CreateChannel();
        _channel.BasicQos(prefetchSize: 0, prefetchCount: (ushort)_stage.PrefetchCount, global: false);

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
                    _logger.LogWarning("Received null event on stage {Stage}, rejecting", _stage.Name);
                    _channel.BasicReject(ea.DeliveryTag, requeue: false);
                    return;
                }

                _logger.LogInformation(
                    "Stage '{Stage}' worker {Worker} received process {ProcessId}",
                    _stage.DisplayName, _workerName, processEvent.ProcessId);

                // Publish process.stage.started event
                _publisher.PublishEvent(new ProcessEvent
                {
                    ProcessId = processEvent.ProcessId,
                    Status = ProcessStatusEnum.StageStarted.ToConstant(),
                    Worker = _workerName,
                    CurrentStage = _stage.Name,
                    Message = processEvent.Message,
                    Priority = processEvent.Priority,
                    Timestamp = DateTime.UtcNow
                }, RabbitMqConstants.ProcessStageStarted);

                // Execute stage logic
                using var scope = _scopeFactory.CreateScope();
                var executor = scope.ServiceProvider.GetRequiredService<ProcessExecutorService>();

                var result = await executor.ExecuteStage(
                    processEvent.ProcessId, _stage.Name, _workerName, processEvent.Message);

                if (result.Success && result.NextStage != null)
                {
                    // Stage completed, forward to next stage
                    _publisher.PublishEvent(new ProcessEvent
                    {
                        ProcessId = processEvent.ProcessId,
                        Status = ProcessStatusEnum.StageCompleted.ToConstant(),
                        Worker = _workerName,
                        CurrentStage = _stage.Name,
                        NextStage = result.NextStage,
                        Message = processEvent.Message,
                        Priority = processEvent.Priority,
                        Timestamp = DateTime.UtcNow
                    }, RabbitMqConstants.ProcessStageCompleted);

                    // Publish to pipeline exchange for next stage
                    _publisher.PublishToPipeline(new ProcessEvent
                    {
                        ProcessId = processEvent.ProcessId,
                        Status = ProcessStatusEnum.Queued.ToConstant(),
                        Worker = null,
                        CurrentStage = result.NextStage,
                        Message = processEvent.Message,
                        Priority = processEvent.Priority,
                        Timestamp = DateTime.UtcNow
                    }, $"pipeline.{result.NextStage}", (byte)processEvent.Priority);

                    _logger.LogInformation(
                        "Process {ProcessId} forwarded from '{Current}' to '{Next}'",
                        processEvent.ProcessId, _stage.Name, result.NextStage);
                }
                else if (result.Success)
                {
                    // Final stage — process finished
                    _publisher.PublishEvent(new ProcessEvent
                    {
                        ProcessId = processEvent.ProcessId,
                        Status = ProcessStatusEnum.Finished.ToConstant(),
                        Worker = _workerName,
                        CurrentStage = _stage.Name,
                        Message = processEvent.Message,
                        Priority = processEvent.Priority,
                        Timestamp = DateTime.UtcNow
                    }, RabbitMqConstants.ProcessFinished);

                    _logger.LogInformation(
                        "Process {ProcessId} finished at stage '{Stage}'",
                        processEvent.ProcessId, _stage.Name);
                }
                else
                {
                    // Stage failed — publish failure and start compensation
                    _publisher.PublishEvent(new ProcessEvent
                    {
                        ProcessId = processEvent.ProcessId,
                        Status = ProcessStatusEnum.Failed.ToConstant(),
                        Worker = _workerName,
                        CurrentStage = _stage.Name,
                        ErrorMessage = result.ErrorMessage,
                        Message = processEvent.Message,
                        Priority = processEvent.Priority,
                        Timestamp = DateTime.UtcNow
                    }, RabbitMqConstants.ProcessFailed);

                    // Start compensation
                    _publisher.PublishEvent(new ProcessEvent
                    {
                        ProcessId = processEvent.ProcessId,
                        Status = ProcessStatusEnum.Compensating.ToConstant(),
                        Worker = _workerName,
                        CurrentStage = _stage.Name,
                        ErrorMessage = result.ErrorMessage,
                        Message = processEvent.Message,
                        Priority = processEvent.Priority,
                        Timestamp = DateTime.UtcNow
                    }, RabbitMqConstants.ProcessCompensating);

                    _logger.LogWarning(
                        "Process {ProcessId} failed at stage '{Stage}': {Error}. Compensation initiated.",
                        processEvent.ProcessId, _stage.Name, result.ErrorMessage);
                }

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in stage '{Stage}' handler", _stage.Name);

                if (retryCount < _stage.MaxRetries)
                {
                    RetryMessage(_channel, ea, retryCount);
                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                else
                {
                    _channel.BasicReject(ea.DeliveryTag, requeue: false);
                }
            }

            await Task.CompletedTask;
        };

        _channel.BasicConsume(
            queue: _stage.QueueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation(
            "PipelineStageHandler '{Stage}' started as {Worker}, listening on {Queue}",
            _stage.DisplayName, _workerName, _stage.QueueName);

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
        var retryQueueName = $"{_stage.QueueName}.retry";

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.Headers = new Dictionary<string, object>
        {
            { "x-retry-count", currentRetryCount + 1 }
        };

        if (ea.BasicProperties.Priority > 0)
            properties.Priority = ea.BasicProperties.Priority;

        channel.BasicPublish(
            exchange: "",
            routingKey: retryQueueName,
            basicProperties: properties,
            body: ea.Body);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}
