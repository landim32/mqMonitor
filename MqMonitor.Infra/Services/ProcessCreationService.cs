using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MqMonitor.Domain.Enums;
using MqMonitor.Domain.Messaging.Interfaces;
using MqMonitor.Domain.Services.Interfaces;
using MqMonitor.DTO;
using MqMonitor.Infra.Configuration;
using MqMonitor.Infra.Messaging.Contracts;

namespace MqMonitor.Infra.Services;

public class ProcessCreationService : IProcessCreationService
{
    private readonly IMessagePublisher _publisher;
    private readonly PipelineSettings _pipelineSettings;
    private readonly ILogger<ProcessCreationService> _logger;

    public ProcessCreationService(
        IMessagePublisher publisher,
        IOptions<PipelineSettings> pipelineSettings,
        ILogger<ProcessCreationService> logger)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _pipelineSettings = pipelineSettings?.Value ?? throw new ArgumentNullException(nameof(pipelineSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public CreateProcessResponse CreateProcess(CreateProcessRequest request)
    {
        var stage = _pipelineSettings.Stages
            .FirstOrDefault(s => s.Name.Equals(request.StageName, StringComparison.OrdinalIgnoreCase));

        if (stage == null)
        {
            var available = string.Join(", ", _pipelineSettings.Stages.Select(s => s.Name));
            throw new ArgumentException(
                $"Unknown stage '{request.StageName}'. Available stages: {available}");
        }

        var processId = $"proc-{Guid.NewGuid().ToString()[..8]}";
        var timestamp = DateTime.UtcNow;
        var message = request.Message ?? $"Process {processId} created for stage {stage.Name}";

        // Publish process.created event to events exchange (for monitor)
        _publisher.PublishEvent(new ProcessEvent
        {
            ProcessId = processId,
            Status = ProcessStatusEnum.Created.ToConstant(),
            CurrentStage = stage.Name,
            Priority = request.Priority,
            Message = message,
            Timestamp = timestamp
        }, RabbitMqConstants.ProcessCreated);

        // Publish to pipeline exchange for the target stage (for worker)
        _publisher.PublishToPipeline(new ProcessEvent
        {
            ProcessId = processId,
            Status = ProcessStatusEnum.Queued.ToConstant(),
            CurrentStage = stage.Name,
            Priority = request.Priority,
            Message = message,
            Timestamp = timestamp
        }, stage.RoutingKey, (byte)request.Priority);

        _logger.LogInformation(
            "Created process {ProcessId} for stage '{Stage}' with priority {Priority}",
            processId, stage.Name, request.Priority);

        return new CreateProcessResponse
        {
            ProcessId = processId,
            StageName = stage.Name,
            Priority = request.Priority,
            Status = ProcessStatusEnum.Created.ToConstant(),
            CreatedAt = timestamp
        };
    }

    public List<string> GetAvailableStages()
    {
        return _pipelineSettings.Stages.Select(s => s.Name).ToList();
    }
}
