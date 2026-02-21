using MqMonitor.Domain.Entities.Interfaces;

namespace MqMonitor.Domain.Entities;

public class SagaStepModel : ISagaStepModel
{
    public string StepId { get; private set; } = string.Empty;
    public string ProcessId { get; private set; } = string.Empty;
    public string StageName { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public string? Worker { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int StepOrder { get; private set; }

    private SagaStepModel() { }

    public static SagaStepModel Create(
        string processId, string stageName, string? worker, int stepOrder)
    {
        if (string.IsNullOrWhiteSpace(processId))
            throw new ArgumentException("ProcessId cannot be empty.", nameof(processId));
        if (string.IsNullOrWhiteSpace(stageName))
            throw new ArgumentException("StageName cannot be empty.", nameof(stageName));

        return new SagaStepModel
        {
            StepId = Guid.NewGuid().ToString(),
            ProcessId = processId,
            StageName = stageName,
            Status = "STARTED",
            Worker = worker,
            StartedAt = DateTime.UtcNow,
            StepOrder = stepOrder
        };
    }

    public static SagaStepModel Reconstruct(
        string stepId, string processId, string stageName, string status,
        string? worker, DateTime startedAt, DateTime? completedAt,
        string? errorMessage, int stepOrder)
    {
        return new SagaStepModel
        {
            StepId = stepId,
            ProcessId = processId,
            StageName = stageName,
            Status = status,
            Worker = worker,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            ErrorMessage = errorMessage,
            StepOrder = stepOrder
        };
    }

    public void Complete()
    {
        Status = "COMPLETED";
        CompletedAt = DateTime.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        Status = "FAILED";
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkCompensated()
    {
        Status = "COMPENSATED";
        CompletedAt = DateTime.UtcNow;
    }

    public override bool Equals(object? obj) =>
        obj is SagaStepModel other && StepId == other.StepId;

    public override int GetHashCode() => StepId.GetHashCode();
}
