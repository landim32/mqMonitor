using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MqMonitor.Application;
using MqMonitor.Domain.Enums;
using MqMonitor.Domain.Messaging.Interfaces;
using MqMonitor.Infra.Configuration;
using MqMonitor.Infra.Messaging.Contracts;
using MqMonitor.Infra.RabbitMq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddMqMonitor(builder.Configuration);

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var topology = scope.ServiceProvider.GetRequiredService<RabbitMqTopologySetup>();
    topology.Configure();
}

var connectionFactory = host.Services.GetRequiredService<RabbitMqConnectionFactory>();
var publisher = host.Services.GetRequiredService<IMessagePublisher>();
var pipelineSettings = host.Services.GetRequiredService<IOptions<PipelineSettings>>().Value;
var logger = host.Services.GetRequiredService<ILogger<Program>>();

const string TARGET_STAGE = "notification";
var stage = pipelineSettings.Stages.First(s => s.Name == TARGET_STAGE);

const int MIN_DELAY_MS = 1000;
const int MAX_DELAY_MS = 5000;
const int ERROR_PERCENTAGE = 10;
const string? NEXT_STAGE = null;

var workerName = $"worker-{TARGET_STAGE}-{Environment.MachineName}-{Guid.NewGuid().ToString()[..8]}";
logger.LogInformation("Starting {Worker} for stage '{Stage}'", workerName, TARGET_STAGE);

var channel = connectionFactory.CreateChannel();
channel.BasicQos(prefetchSize: 0, prefetchCount: (ushort)stage.PrefetchCount, global: false);

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.Received += async (_, ea) =>
{
    var retryCount = 0;
    if (ea.BasicProperties.Headers?.TryGetValue("x-retry-count", out var rc) == true)
        retryCount = Convert.ToInt32(rc);

    try
    {
        var processEvent = JsonSerializer.Deserialize<ProcessEvent>(
            Encoding.UTF8.GetString(ea.Body.ToArray()));

        if (processEvent == null) { channel.BasicReject(ea.DeliveryTag, requeue: false); return; }

        logger.LogInformation("[{Worker}] Processing {ProcessId} at stage '{Stage}'",
            workerName, processEvent.ProcessId, TARGET_STAGE);

        publisher.PublishEvent(new ProcessEvent
        {
            ProcessId = processEvent.ProcessId,
            Status = ProcessStatusEnum.StageStarted.ToConstant(),
            Worker = workerName, CurrentStage = TARGET_STAGE,
            Message = processEvent.Message, Priority = processEvent.Priority,
            Timestamp = DateTime.UtcNow
        }, RabbitMqConstants.ProcessStageStarted);

        var delay = Random.Shared.Next(MIN_DELAY_MS, MAX_DELAY_MS + 1);
        await Task.Delay(delay);

        if (Random.Shared.Next(100) < ERROR_PERCENTAGE)
        {
            var errorMsg = $"Simulated failure at stage '{TARGET_STAGE}' after {delay}ms";
            logger.LogWarning("[{Worker}] {ProcessId} FAILED: {Error}", workerName, processEvent.ProcessId, errorMsg);

            publisher.PublishEvent(new ProcessEvent
            {
                ProcessId = processEvent.ProcessId, Status = ProcessStatusEnum.Failed.ToConstant(),
                Worker = workerName, CurrentStage = TARGET_STAGE, ErrorMessage = errorMsg,
                Message = processEvent.Message, Priority = processEvent.Priority, Timestamp = DateTime.UtcNow
            }, RabbitMqConstants.ProcessFailed);

            publisher.PublishEvent(new ProcessEvent
            {
                ProcessId = processEvent.ProcessId, Status = ProcessStatusEnum.Compensating.ToConstant(),
                Worker = workerName, CurrentStage = TARGET_STAGE, ErrorMessage = errorMsg,
                Message = processEvent.Message, Priority = processEvent.Priority, Timestamp = DateTime.UtcNow
            }, RabbitMqConstants.ProcessCompensating);
        }
        else if (NEXT_STAGE != null)
        {
            publisher.PublishEvent(new ProcessEvent
            {
                ProcessId = processEvent.ProcessId, Status = ProcessStatusEnum.StageCompleted.ToConstant(),
                Worker = workerName, CurrentStage = TARGET_STAGE, NextStage = NEXT_STAGE,
                Message = processEvent.Message, Priority = processEvent.Priority, Timestamp = DateTime.UtcNow
            }, RabbitMqConstants.ProcessStageCompleted);

            publisher.PublishToPipeline(new ProcessEvent
            {
                ProcessId = processEvent.ProcessId, Status = ProcessStatusEnum.Queued.ToConstant(),
                CurrentStage = NEXT_STAGE, Message = processEvent.Message,
                Priority = processEvent.Priority, Timestamp = DateTime.UtcNow
            }, $"pipeline.{NEXT_STAGE}", (byte)processEvent.Priority);

            logger.LogInformation("[{Worker}] {ProcessId} completed → forwarded to '{Next}'",
                workerName, processEvent.ProcessId, NEXT_STAGE);
        }
        else
        {
            publisher.PublishEvent(new ProcessEvent
            {
                ProcessId = processEvent.ProcessId, Status = ProcessStatusEnum.Finished.ToConstant(),
                Worker = workerName, CurrentStage = TARGET_STAGE,
                Message = processEvent.Message, Priority = processEvent.Priority, Timestamp = DateTime.UtcNow
            }, RabbitMqConstants.ProcessFinished);

            logger.LogInformation("[{Worker}] {ProcessId} FINISHED (final stage)", workerName, processEvent.ProcessId);
        }

        channel.BasicAck(ea.DeliveryTag, multiple: false);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[{Worker}] Error processing message", workerName);
        if (retryCount < stage.MaxRetries)
        {
            var retryQueueName = $"{stage.QueueName}.retry";
            var props = channel.CreateBasicProperties();
            props.Persistent = true;
            props.Headers = new Dictionary<string, object> { { "x-retry-count", retryCount + 1 } };
            if (ea.BasicProperties.Priority > 0) props.Priority = ea.BasicProperties.Priority;
            channel.BasicPublish(exchange: "", routingKey: retryQueueName, basicProperties: props, body: ea.Body);
            channel.BasicAck(ea.DeliveryTag, multiple: false);
        }
        else { channel.BasicReject(ea.DeliveryTag, requeue: false); }
    }
};

channel.BasicConsume(queue: stage.QueueName, autoAck: false, consumer: consumer);
logger.LogInformation("[{Worker}] Listening on queue '{Queue}'", workerName, stage.QueueName);

await host.RunAsync();
