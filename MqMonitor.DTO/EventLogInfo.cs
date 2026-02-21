namespace MqMonitor.DTO;

public class EventLogInfo
{
    public string EventId { get; set; } = string.Empty;
    public string ProcessId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
