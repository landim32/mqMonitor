using MqMonitor.Domain.Entities.Interfaces;

namespace MqMonitor.Domain.Entities;

public class EventLogModel : IEventLogModel
{
    public string EventId { get; private set; } = string.Empty;
    public string ProcessId { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; }

    private EventLogModel() { }

    public static EventLogModel Create(
        string eventId, string processId, string type, string payload, DateTime timestamp)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            throw new ArgumentException("EventId cannot be empty.", nameof(eventId));
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Type cannot be empty.", nameof(type));

        return new EventLogModel
        {
            EventId = eventId,
            ProcessId = processId,
            Type = type,
            Payload = payload,
            Timestamp = timestamp
        };
    }

    public static EventLogModel Reconstruct(
        string eventId, string processId, string type, string payload, DateTime timestamp)
    {
        return new EventLogModel
        {
            EventId = eventId,
            ProcessId = processId,
            Type = type,
            Payload = payload,
            Timestamp = timestamp
        };
    }

    public override bool Equals(object? obj) =>
        obj is EventLogModel other && EventId == other.EventId;

    public override int GetHashCode() => EventId.GetHashCode();
}
