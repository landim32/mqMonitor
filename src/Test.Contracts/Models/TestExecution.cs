namespace Test.Contracts.Models;

public class TestExecution
{
    public string TestId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Worker { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
