using MqMonitor.DTO;

namespace MqMonitor.Domain.Services.Interfaces;

public interface IProcessQueryService
{
    Task<List<ProcessExecutionInfo>> GetAllExecutionsAsync();
    Task<ProcessExecutionInfo?> GetExecutionAsync(string processId);
    Task<List<EventLogInfo>> GetEventsByProcessIdAsync(string processId);
    Task<ProcessMetricsInfo> GetMetricsAsync();
    Task<List<SagaStepInfo>> GetSagaStepsAsync(string processId);
    Task<List<ProcessExecutionInfo>> GetExecutionsByStageAsync(string stageName);
    Task<List<ProcessExecutionInfo>> GetExecutionsByStatusAsync(string status);
    Task<bool> UpdatePriorityAsync(string processId, int priority);
}
