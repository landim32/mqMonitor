namespace MqMonitor.Infra.Messaging.Contracts;

public class ChangePriorityCommand
{
    public string ProcessId { get; set; } = string.Empty;
    public int Priority { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
