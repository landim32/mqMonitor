namespace MqMonitor.Domain.Services.Interfaces;

public interface IEventProjectionService
{
    Task<bool> IsEventProcessedAsync(string eventId);
    Task ProjectEventAsync(string eventId, string processId, string status,
        DateTime timestamp, string? worker, string? errorMessage, string eventType, string payload,
        string? message = null, string? currentStage = null, int priority = 0, string? sagaStatus = null);
}
