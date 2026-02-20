using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Test.Contracts.Constants;
using Test.Contracts.Events;
using Test.Contracts.Models;
using Test.Infrastructure.Database;

namespace Test.Monitor.Services;

public class EventProjectionService
{
    private readonly MonitorDbContext _db;
    private readonly ILogger<EventProjectionService> _logger;

    public EventProjectionService(
        MonitorDbContext db,
        ILogger<EventProjectionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<bool> IsEventProcessed(string eventId)
    {
        return await _db.EventLogs.AnyAsync(e => e.EventId == eventId);
    }

    public async Task ProjectEvent(TestEvent testEvent, string eventType)
    {
        // Idempotency check
        if (await IsEventProcessed(testEvent.EventId))
        {
            _logger.LogWarning(
                "Event {EventId} already processed, skipping (idempotent)",
                testEvent.EventId);
            return;
        }

        // Log event
        var eventLog = new EventLog
        {
            EventId = testEvent.EventId,
            Type = eventType,
            Payload = JsonSerializer.Serialize(testEvent),
            Timestamp = testEvent.Timestamp
        };

        _db.EventLogs.Add(eventLog);

        // Project into TestExecution read model
        var execution = await _db.TestExecutions.FindAsync(testEvent.TestId);

        if (execution == null)
        {
            execution = new TestExecution
            {
                TestId = testEvent.TestId,
                Status = testEvent.Status,
                Worker = testEvent.Worker,
                UpdatedAt = testEvent.Timestamp
            };
            _db.TestExecutions.Add(execution);
        }
        else
        {
            execution.Status = testEvent.Status;
            execution.UpdatedAt = testEvent.Timestamp;

            if (testEvent.Worker != null)
                execution.Worker = testEvent.Worker;
        }

        // Set timestamps based on status
        switch (eventType)
        {
            case RabbitMqConstants.TestStarted:
                execution.StartedAt = testEvent.Timestamp;
                break;

            case RabbitMqConstants.TestFinished:
            case RabbitMqConstants.TestFailed:
            case RabbitMqConstants.TestCancelled:
                execution.FinishedAt = testEvent.Timestamp;
                break;
        }

        if (testEvent.ErrorMessage != null)
            execution.ErrorMessage = testEvent.ErrorMessage;

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Projected event {EventId} ({Type}) for test {TestId}",
            testEvent.EventId, eventType, testEvent.TestId);
    }
}
