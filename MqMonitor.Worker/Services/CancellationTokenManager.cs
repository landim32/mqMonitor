using System.Collections.Concurrent;

namespace MqMonitor.Worker.Services;

public class CancellationTokenManager
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _tokens = new();
    private readonly ILogger<CancellationTokenManager> _logger;

    public CancellationTokenManager(ILogger<CancellationTokenManager> logger)
    {
        _logger = logger;
    }

    public CancellationTokenSource Register(string processId)
    {
        var cts = new CancellationTokenSource();
        _tokens[processId] = cts;
        _logger.LogDebug("Registered cancellation token for process {ProcessId}", processId);
        return cts;
    }

    public bool TryCancel(string processId)
    {
        if (_tokens.TryGetValue(processId, out var cts))
        {
            cts.Cancel();
            _logger.LogInformation("Cancellation requested for process {ProcessId}", processId);
            return true;
        }

        _logger.LogWarning(
            "No active execution found for process {ProcessId}, cancel ignored",
            processId);
        return false;
    }

    public void Unregister(string processId)
    {
        if (_tokens.TryRemove(processId, out var cts))
        {
            cts.Dispose();
            _logger.LogDebug("Unregistered cancellation token for process {ProcessId}", processId);
        }
    }
}
