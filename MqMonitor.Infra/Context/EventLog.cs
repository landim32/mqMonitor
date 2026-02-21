namespace MqMonitor.Infra.Context;

public partial class EventLog
{
    public string EventId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string ProcessId { get; set; } = string.Empty;
}
