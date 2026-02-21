using MqMonitor.Domain.Enums;
using MqMonitor.Domain.Messaging.Interfaces;
using MqMonitor.Infra.Configuration;
using MqMonitor.Infra.Messaging.Contracts;

namespace MqMonitor.Worker.Services;

public class ProcessExecutorService
{
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<ProcessExecutorService> _logger;
    private readonly CancellationTokenManager _cancellationManager;

    public ProcessExecutorService(
        IMessagePublisher publisher,
        CancellationTokenManager cancellationManager,
        ILogger<ProcessExecutorService> logger)
    {
        _publisher = publisher;
        _cancellationManager = cancellationManager;
        _logger = logger;
    }

    public async Task ExecuteProcess(string processId, string workerName)
    {
        var cts = _cancellationManager.Register(processId);

        try
        {
            // Publish process.started
            _publisher.PublishEvent(new ProcessEvent
            {
                ProcessId = processId,
                Status = ProcessStatusEnum.Started.ToConstant(),
                Worker = workerName,
                Timestamp = DateTime.UtcNow
            }, RabbitMqConstants.ProcessStarted);

            _logger.LogInformation("Process {ProcessId} started on worker {Worker}", processId, workerName);

            // Simulate process execution (5-15 seconds)
            var executionTime = Random.Shared.Next(5000, 15000);
            var stepCount = 10;
            var stepDelay = executionTime / stepCount;

            for (int i = 0; i < stepCount; i++)
            {
                cts.Token.ThrowIfCancellationRequested();
                await Task.Delay(stepDelay, cts.Token);
            }

            // Simulate random failures (10% chance)
            if (Random.Shared.Next(100) < 10)
            {
                throw new InvalidOperationException("Simulated process failure");
            }

            // Publish process.finished
            _publisher.PublishEvent(new ProcessEvent
            {
                ProcessId = processId,
                Status = ProcessStatusEnum.Finished.ToConstant(),
                Worker = workerName,
                Timestamp = DateTime.UtcNow
            }, RabbitMqConstants.ProcessFinished);

            _logger.LogInformation("Process {ProcessId} finished successfully", processId);
        }
        catch (OperationCanceledException)
        {
            // Publish process.cancelled
            _publisher.PublishEvent(new ProcessEvent
            {
                ProcessId = processId,
                Status = ProcessStatusEnum.Cancelled.ToConstant(),
                Worker = workerName,
                Timestamp = DateTime.UtcNow
            }, RabbitMqConstants.ProcessCancelled);

            _logger.LogInformation("Process {ProcessId} was cancelled", processId);
        }
        catch (Exception ex)
        {
            // Publish process.failed
            _publisher.PublishEvent(new ProcessEvent
            {
                ProcessId = processId,
                Status = ProcessStatusEnum.Failed.ToConstant(),
                Worker = workerName,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow
            }, RabbitMqConstants.ProcessFailed);

            _logger.LogError(ex, "Process {ProcessId} failed", processId);
        }
        finally
        {
            _cancellationManager.Unregister(processId);
        }
    }

    public async Task<StageResult> ExecuteStage(
        string processId, string stageName, string workerName, string? message)
    {
        var cts = _cancellationManager.Register(processId);

        try
        {
            _logger.LogInformation(
                "Executing stage '{Stage}' for process {ProcessId} on worker {Worker}",
                stageName, processId, workerName);

            // Simulate stage execution (2-8 seconds)
            var executionTime = Random.Shared.Next(2000, 8000);
            var stepCount = 5;
            var stepDelay = executionTime / stepCount;

            for (int i = 0; i < stepCount; i++)
            {
                cts.Token.ThrowIfCancellationRequested();
                await Task.Delay(stepDelay, cts.Token);
            }

            // Simulate random failures (10% chance)
            if (Random.Shared.Next(100) < 10)
            {
                return new StageResult(false, null, $"Simulated failure at stage '{stageName}'");
            }

            // Simulate dynamic routing decision:
            // 30% chance to forward to another stage, 70% chance this is the final stage
            string? nextStage = null;
            if (Random.Shared.Next(100) < 30)
            {
                // Pick a random stage name to forward to (simulated)
                var possibleStages = new[] { "relatorio", "conta", "rotina" };
                var candidates = possibleStages.Where(s => s != stageName).ToArray();
                if (candidates.Length > 0)
                {
                    nextStage = candidates[Random.Shared.Next(candidates.Length)];
                }
            }

            _logger.LogInformation(
                "Stage '{Stage}' completed for process {ProcessId}. NextStage: {NextStage}",
                stageName, processId, nextStage ?? "(final)");

            return new StageResult(true, nextStage, null);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Stage '{Stage}' cancelled for process {ProcessId}", stageName, processId);
            return new StageResult(false, null, "Stage execution was cancelled");
        }
        finally
        {
            _cancellationManager.Unregister(processId);
        }
    }
}

public record StageResult(bool Success, string? NextStage, string? ErrorMessage);
