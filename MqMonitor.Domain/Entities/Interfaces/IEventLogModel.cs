namespace MqMonitor.Domain.Entities.Interfaces;

public interface IEventLogModel
{
    string EventId { get; }
    string ProcessId { get; }
    string Type { get; }
    string Payload { get; }
    DateTime Timestamp { get; }
}
