namespace MqMonitor.DTO;

public class CreateProcessRequest
{
    public string StageName { get; set; } = string.Empty;
    public string? Message { get; set; }
    public int Priority { get; set; } = 0;
}
