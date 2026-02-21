using Microsoft.Extensions.Logging;
using MqMonitor.Domain.Entities;
using MqMonitor.Domain.Entities.Interfaces;
using MqMonitor.Domain.Services.Interfaces;
using MqMonitor.Infra.Configuration;
using MqMonitor.Infra.Interfaces.Repository;

namespace MqMonitor.Infra.Services;

public class EventProjectionService : IEventProjectionService
{
    private readonly IProcessExecutionRepository<IProcessExecutionModel> _executionRepo;
    private readonly IEventLogRepository<IEventLogModel> _eventLogRepo;
    private readonly ISagaStepRepository<ISagaStepModel> _sagaStepRepo;
    private readonly ILogger<EventProjectionService> _logger;

    public EventProjectionService(
        IProcessExecutionRepository<IProcessExecutionModel> executionRepo,
        IEventLogRepository<IEventLogModel> eventLogRepo,
        ISagaStepRepository<ISagaStepModel> sagaStepRepo,
        ILogger<EventProjectionService> logger)
    {
        _executionRepo = executionRepo ?? throw new ArgumentNullException(nameof(executionRepo));
        _eventLogRepo = eventLogRepo ?? throw new ArgumentNullException(nameof(eventLogRepo));
        _sagaStepRepo = sagaStepRepo ?? throw new ArgumentNullException(nameof(sagaStepRepo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> IsEventProcessedAsync(string eventId)
    {
        return await _eventLogRepo.ExistsAsync(eventId);
    }

    public async Task ProjectEventAsync(
        string eventId, string processId, string status,
        DateTime timestamp, string? worker, string? errorMessage,
        string eventType, string payload,
        string? message = null, string? currentStage = null, int priority = 0, string? sagaStatus = null)
    {
        // Idempotency check
        if (await _eventLogRepo.ExistsAsync(eventId))
        {
            _logger.LogWarning(
                "Event {EventId} already processed, skipping (idempotent)",
                eventId);
            return;
        }

        // Log event
        var eventLog = EventLogModel.Create(eventId, processId, eventType, payload, timestamp);
        await _eventLogRepo.InsertAsync(eventLog);

        // Project into ProcessExecution read model
        var existing = await _executionRepo.GetByIdAsync(processId);

        if (existing == null)
        {
            var execution = ProcessExecutionModel.CreateFromEvent(
                processId, status, worker, timestamp,
                message, currentStage, priority);
            execution.ApplyEvent(status, worker, timestamp, errorMessage, eventType, currentStage, sagaStatus);
            await _executionRepo.InsertAsync(execution);
        }
        else
        {
            var mutableModel = ProcessExecutionModel.Reconstruct(
                existing.ProcessId, existing.Status, existing.Worker,
                existing.StartedAt, existing.FinishedAt,
                existing.UpdatedAt, existing.ErrorMessage,
                existing.Message, existing.CurrentStage,
                existing.Priority, existing.SagaStatus);

            mutableModel.ApplyEvent(status, worker, timestamp, errorMessage, eventType, currentStage, sagaStatus);
            await _executionRepo.UpdateAsync(mutableModel);
        }

        // Project saga steps for stage events
        await ProjectSagaStepAsync(eventType, processId, currentStage, worker, timestamp, errorMessage);

        _logger.LogInformation(
            "Projected event {EventId} ({Type}) for process {ProcessId}",
            eventId, eventType, processId);
    }

    private async Task ProjectSagaStepAsync(
        string eventType, string processId, string? currentStage,
        string? worker, DateTime timestamp, string? errorMessage)
    {
        if (string.IsNullOrEmpty(currentStage))
            return;

        switch (eventType)
        {
            case RabbitMqConstants.ProcessStageStarted:
            {
                // Determine step order based on existing steps
                var lastStep = await _sagaStepRepo.GetLastStepAsync(processId);
                var stepOrder = (lastStep?.StepOrder ?? 0) + 1;

                var sagaStep = SagaStepModel.Create(processId, currentStage, worker, stepOrder);
                await _sagaStepRepo.InsertAsync(sagaStep);

                _logger.LogInformation(
                    "Created saga step #{Order} ({Stage}) for process {ProcessId}",
                    stepOrder, currentStage, processId);
                break;
            }

            case RabbitMqConstants.ProcessStageCompleted:
            {
                var lastStep = await _sagaStepRepo.GetLastStepAsync(processId);
                if (lastStep != null && lastStep.StageName == currentStage && lastStep.Status == "STARTED")
                {
                    var mutableStep = SagaStepModel.Reconstruct(
                        lastStep.StepId, lastStep.ProcessId, lastStep.StageName,
                        lastStep.Status, lastStep.Worker, lastStep.StartedAt,
                        lastStep.CompletedAt, lastStep.ErrorMessage, lastStep.StepOrder);
                    mutableStep.Complete();
                    await _sagaStepRepo.UpdateAsync(mutableStep);

                    _logger.LogInformation(
                        "Completed saga step #{Order} ({Stage}) for process {ProcessId}",
                        lastStep.StepOrder, currentStage, processId);
                }
                break;
            }

            case RabbitMqConstants.ProcessFailed:
            {
                var lastStep = await _sagaStepRepo.GetLastStepAsync(processId);
                if (lastStep != null && lastStep.StageName == currentStage && lastStep.Status == "STARTED")
                {
                    var mutableStep = SagaStepModel.Reconstruct(
                        lastStep.StepId, lastStep.ProcessId, lastStep.StageName,
                        lastStep.Status, lastStep.Worker, lastStep.StartedAt,
                        lastStep.CompletedAt, lastStep.ErrorMessage, lastStep.StepOrder);
                    mutableStep.Fail(errorMessage ?? "Unknown error");
                    await _sagaStepRepo.UpdateAsync(mutableStep);

                    _logger.LogInformation(
                        "Failed saga step #{Order} ({Stage}) for process {ProcessId}",
                        lastStep.StepOrder, currentStage, processId);
                }
                break;
            }

            case RabbitMqConstants.ProcessCompensated:
            {
                // Mark all completed steps as compensated
                var steps = await _sagaStepRepo.GetByProcessIdAsync(processId);
                foreach (var step in steps.Where(s => s.Status == "COMPLETED"))
                {
                    var mutableStep = SagaStepModel.Reconstruct(
                        step.StepId, step.ProcessId, step.StageName,
                        step.Status, step.Worker, step.StartedAt,
                        step.CompletedAt, step.ErrorMessage, step.StepOrder);
                    mutableStep.MarkCompensated();
                    await _sagaStepRepo.UpdateAsync(mutableStep);
                }
                break;
            }
        }
    }
}
