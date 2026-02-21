namespace MqMonitor.DTO;

public class QueueStatusInfo
{
    public string QueueName { get; set; } = string.Empty;
    public string? StageName { get; set; }
    public string? DisplayName { get; set; }
    public int MessageCount { get; set; }
    public int ConsumerCount { get; set; }
    public double PublishRate { get; set; }
    public double DeliverRate { get; set; }
    public double AckRate { get; set; }
    public string State { get; set; } = string.Empty;
}
