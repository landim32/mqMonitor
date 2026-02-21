using Microsoft.AspNetCore.SignalR;

namespace MqMonitor.API.Hubs;

public class MonitorHub : Hub
{
    private readonly ILogger<MonitorHub> _logger;

    public MonitorHub(ILogger<MonitorHub> logger)
    {
        _logger = logger;
    }

    public async Task SubscribeToProcess(string processId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"process-{processId}");
        _logger.LogInformation("Client {ConnectionId} subscribed to process {ProcessId}",
            Context.ConnectionId, processId);
    }

    public async Task UnsubscribeFromProcess(string processId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"process-{processId}");
    }

    public async Task SubscribeToQueue(string queueName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"queue-{queueName}");
        _logger.LogInformation("Client {ConnectionId} subscribed to queue {QueueName}",
            Context.ConnectionId, queueName);
    }

    public async Task UnsubscribeFromQueue(string queueName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"queue-{queueName}");
    }

    public async Task SubscribeToAll()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "all");
        _logger.LogInformation("Client {ConnectionId} subscribed to all updates",
            Context.ConnectionId);
    }

    public async Task UnsubscribeFromAll()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all");
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
