using Microsoft.EntityFrameworkCore;
using Test.Contracts.Models;
using Test.Infrastructure.Database;

namespace Test.Monitor.Services;

public class TestQueryService
{
    private readonly MonitorDbContext _db;

    public TestQueryService(MonitorDbContext db)
    {
        _db = db;
    }

    public async Task<List<TestExecution>> GetAllExecutions()
    {
        return await _db.TestExecutions
            .OrderByDescending(e => e.UpdatedAt)
            .ToListAsync();
    }

    public async Task<TestExecution?> GetExecution(string testId)
    {
        return await _db.TestExecutions.FindAsync(testId);
    }

    public async Task<List<EventLog>> GetEventsByTestId(string testId)
    {
        return await _db.EventLogs
            .Where(e => EF.Functions.JsonContains(
                e.Payload,
                $"{{\"TestId\":\"{testId}\"}}"))
            .OrderBy(e => e.Timestamp)
            .ToListAsync();
    }

    public async Task<TestMetrics> GetMetrics()
    {
        var executions = await _db.TestExecutions.ToListAsync();

        var completedWithTimes = executions
            .Where(e => e.StartedAt.HasValue && e.FinishedAt.HasValue)
            .ToList();

        var totalExecuted = executions.Count;
        var inProgress = executions.Count(e =>
            e.Status == TestStatus.Started || e.Status == TestStatus.Created);
        var failed = executions.Count(e => e.Status == TestStatus.Failed);
        var cancelled = executions.Count(e => e.Status == TestStatus.Cancelled);
        var finished = executions.Count(e => e.Status == TestStatus.Finished);

        var averageMs = completedWithTimes.Count > 0
            ? completedWithTimes.Average(e =>
                (e.FinishedAt!.Value - e.StartedAt!.Value).TotalMilliseconds)
            : 0;

        var errorRate = totalExecuted > 0
            ? (double)failed / totalExecuted * 100
            : 0;

        return new TestMetrics
        {
            TotalExecuted = totalExecuted,
            InProgress = inProgress,
            Failed = failed,
            Cancelled = cancelled,
            Finished = finished,
            AverageExecutionTimeMs = Math.Round(averageMs, 2),
            ErrorRate = Math.Round(errorRate, 2)
        };
    }
}
