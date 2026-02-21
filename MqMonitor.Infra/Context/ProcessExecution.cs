namespace MqMonitor.Infra.Context;

public partial class ProcessExecution
{
    public string ProcessId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Worker { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
    public string? CurrentStage { get; set; }
    public int Priority { get; set; }
    public string? SagaStatus { get; set; }
}
