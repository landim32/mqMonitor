using MqMonitor.Domain.Entities.Interfaces;

namespace MqMonitor.Domain.Entities;

public class ProcessExecutionModel : IProcessExecutionModel
{
    public string ProcessId { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public string? Worker { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? FinishedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? Message { get; private set; }
    public string? CurrentStage { get; private set; }
    public int Priority { get; private set; }
    public string? SagaStatus { get; private set; }

    public bool IsTerminal => Status is "FINISHED" or "FAILED" or "CANCELLED";

    private ProcessExecutionModel() { }

    public static ProcessExecutionModel CreateFromEvent(
        string processId, string status, string? worker, DateTime timestamp,
        string? message = null, string? currentStage = null, int priority = 0)
    {
        if (string.IsNullOrWhiteSpace(processId))
            throw new ArgumentException("ProcessId cannot be empty.", nameof(processId));

        return new ProcessExecutionModel
        {
            ProcessId = processId,
            Status = status,
            Worker = worker,
            UpdatedAt = timestamp,
            Message = message,
            CurrentStage = currentStage,
            Priority = priority
        };
    }

    public static ProcessExecutionModel Reconstruct(
        string processId, string status, string? worker,
        DateTime? startedAt, DateTime? finishedAt,
        DateTime updatedAt, string? errorMessage,
        string? message, string? currentStage,
        int priority, string? sagaStatus)
    {
        return new ProcessExecutionModel
        {
            ProcessId = processId,
            Status = status,
            Worker = worker,
            StartedAt = startedAt,
            FinishedAt = finishedAt,
            UpdatedAt = updatedAt,
            ErrorMessage = errorMessage,
            Message = message,
            CurrentStage = currentStage,
            Priority = priority,
            SagaStatus = sagaStatus
        };
    }

    public void ApplyEvent(
        string status, string? worker, DateTime timestamp,
        string? errorMessage, string eventType,
        string? currentStage = null, string? sagaStatus = null)
    {
        Status = status;
        UpdatedAt = timestamp;

        if (worker != null)
            Worker = worker;

        if (errorMessage != null)
            ErrorMessage = errorMessage;

        if (currentStage != null)
            CurrentStage = currentStage;

        if (sagaStatus != null)
            SagaStatus = sagaStatus;

        switch (eventType)
        {
            case "process.started":
                StartedAt = timestamp;
                break;
            case "process.finished":
            case "process.failed":
            case "process.cancelled":
                FinishedAt = timestamp;
                break;
        }
    }

    public void UpdatePriority(int priority)
    {
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    public override bool Equals(object? obj) =>
        obj is ProcessExecutionModel other && ProcessId == other.ProcessId;

    public override int GetHashCode() => ProcessId.GetHashCode();
}
