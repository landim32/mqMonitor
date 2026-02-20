using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Test.Worker.Services;

public class CancellationTokenManager
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _tokens = new();
    private readonly ILogger<CancellationTokenManager> _logger;

    public CancellationTokenManager(ILogger<CancellationTokenManager> logger)
    {
        _logger = logger;
    }

    public CancellationTokenSource Register(string testId)
    {
        var cts = new CancellationTokenSource();
        _tokens[testId] = cts;
        _logger.LogDebug("Registered cancellation token for test {TestId}", testId);
        return cts;
    }

    public bool TryCancel(string testId)
    {
        if (_tokens.TryGetValue(testId, out var cts))
        {
            cts.Cancel();
            _logger.LogInformation("Cancellation requested for test {TestId}", testId);
            return true;
        }

        _logger.LogWarning(
            "No active execution found for test {TestId}, cancel ignored",
            testId);
        return false;
    }

    public void Unregister(string testId)
    {
        if (_tokens.TryRemove(testId, out var cts))
        {
            cts.Dispose();
            _logger.LogDebug("Unregistered cancellation token for test {TestId}", testId);
        }
    }
}
