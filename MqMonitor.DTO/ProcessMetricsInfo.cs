namespace MqMonitor.DTO;

public class ProcessMetricsInfo
{
    public int TotalExecuted { get; set; }
    public int InProgress { get; set; }
    public int Failed { get; set; }
    public int Cancelled { get; set; }
    public int Finished { get; set; }
    public double AverageExecutionTimeMs { get; set; }
    public double ErrorRate { get; set; }
    public Dictionary<string, int> ByStage { get; set; } = new();
}
