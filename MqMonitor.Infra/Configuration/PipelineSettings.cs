namespace MqMonitor.Infra.Configuration;

public class PipelineSettings
{
    public const string SectionName = "Pipeline";
    public List<StageDefinition> Stages { get; set; } = new();
    public string PipelineExchange { get; set; } = "processes.pipeline";
}

public class StageDefinition
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
    public string RoutingKey { get; set; } = string.Empty;
    public int MaxPriority { get; set; } = 10;
    public int PrefetchCount { get; set; } = 1;
    public string? DlqName { get; set; }
    public int RetryDelayMs { get; set; } = 5000;
    public int MaxRetries { get; set; } = 3;
}
