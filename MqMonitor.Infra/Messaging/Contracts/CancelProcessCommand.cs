namespace MqMonitor.Infra.Messaging.Contracts;

public class CancelProcessCommand
{
    public string CommandId { get; set; } = Guid.NewGuid().ToString();
    public string ProcessId { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}
