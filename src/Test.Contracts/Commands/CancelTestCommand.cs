namespace Test.Contracts.Commands;

public class CancelTestCommand
{
    public string CommandId { get; set; } = Guid.NewGuid().ToString();
    public string TestId { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}
