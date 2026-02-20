namespace Test.Contracts.Events;

public class TestEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string TestId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Worker { get; set; }
    public string? ErrorMessage { get; set; }
}
