namespace MqMonitor.DTO;

public class CreateProcessResponse
{
    public string ProcessId { get; set; } = string.Empty;
    public string StageName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
