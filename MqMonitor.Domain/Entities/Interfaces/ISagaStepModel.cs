namespace MqMonitor.Domain.Entities.Interfaces;

public interface ISagaStepModel
{
    string StepId { get; }
    string ProcessId { get; }
    string StageName { get; }
    string Status { get; }
    string? Worker { get; }
    DateTime StartedAt { get; }
    DateTime? CompletedAt { get; }
    string? ErrorMessage { get; }
    int StepOrder { get; }
}
