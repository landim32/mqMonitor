using MqMonitor.DTO;

namespace MqMonitor.Domain.Services.Interfaces;

public interface IRabbitMqManagementService
{
    Task<List<QueueStatusInfo>> GetAllQueueStatusAsync();
    Task<QueueStatusInfo?> GetQueueStatusAsync(string queueName);
    Task<PipelineStatusInfo> GetPipelineStatusAsync();
}
