namespace MqMonitor.Infra.Messaging.Contracts;

public class ProcessEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string ProcessId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Worker { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
    public string? CurrentStage { get; set; }
    public int Priority { get; set; }
    public string? NextStage { get; set; }
}
