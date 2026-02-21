namespace MqMonitor.Infra.Context;

public partial class SagaStep
{
    public string StepId { get; set; } = string.Empty;
    public string ProcessId { get; set; } = string.Empty;
    public string StageName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Worker { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int StepOrder { get; set; }
}
