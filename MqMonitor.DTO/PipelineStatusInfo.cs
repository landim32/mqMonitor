namespace MqMonitor.DTO;

public class PipelineStatusInfo
{
    public List<QueueStatusInfo> Stages { get; set; } = new();
    public List<QueueStatusInfo> SystemQueues { get; set; } = new();
    public int TotalMessages { get; set; }
    public int TotalConsumers { get; set; }
}
