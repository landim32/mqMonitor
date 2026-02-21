using Microsoft.AspNetCore.SignalR;
using MqMonitor.API.Hubs;
using MqMonitor.Domain.Services.Interfaces;

namespace MqMonitor.API.Services;

public class QueueStatsBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<MonitorHub> _hubContext;
    private readonly ILogger<QueueStatsBackgroundService> _logger;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    public QueueStatsBackgroundService(
        IServiceScopeFactory scopeFactory,
        IHubContext<MonitorHub> hubContext,
        ILogger<QueueStatsBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("QueueStatsBackgroundService started, polling every {Interval}s",
            PollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var managementService = scope.ServiceProvider
                    .GetRequiredService<IRabbitMqManagementService>();

                var pipelineStatus = await managementService.GetPipelineStatusAsync();

                await _hubContext.Clients.Group("all")
                    .SendAsync("QueueStatsUpdated", pipelineStatus, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling queue stats");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }
}
