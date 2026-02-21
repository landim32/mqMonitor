namespace MqMonitor.Domain.Entities.Interfaces;

public interface IProcessExecutionModel
{
    string ProcessId { get; }
    string Status { get; }
    string? Worker { get; }
    DateTime? StartedAt { get; }
    DateTime? FinishedAt { get; }
    DateTime UpdatedAt { get; }
    string? ErrorMessage { get; }
    string? Message { get; }
    string? CurrentStage { get; }
    int Priority { get; }
    string? SagaStatus { get; }
    bool IsTerminal { get; }
}
