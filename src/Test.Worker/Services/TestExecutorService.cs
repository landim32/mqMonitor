using Microsoft.Extensions.Logging;
using Test.Contracts.Constants;
using Test.Contracts.Events;
using Test.Contracts.Models;
using Test.Infrastructure.RabbitMq;

namespace Test.Worker.Services;

public class TestExecutorService
{
    private readonly RabbitMqPublisher _publisher;
    private readonly ILogger<TestExecutorService> _logger;
    private readonly CancellationTokenManager _cancellationManager;

    public TestExecutorService(
        RabbitMqPublisher publisher,
        CancellationTokenManager cancellationManager,
        ILogger<TestExecutorService> logger)
    {
        _publisher = publisher;
        _cancellationManager = cancellationManager;
        _logger = logger;
    }

    public async Task ExecuteTest(string testId, string workerName)
    {
        var cts = _cancellationManager.Register(testId);

        try
        {
            // Publish test.started
            _publisher.PublishEvent(new TestEvent
            {
                TestId = testId,
                Status = TestStatus.Started,
                Worker = workerName,
                Timestamp = DateTime.UtcNow
            }, RabbitMqConstants.TestStarted);

            _logger.LogInformation("Test {TestId} started on worker {Worker}", testId, workerName);

            // Simulate test execution (5-15 seconds)
            var executionTime = Random.Shared.Next(5000, 15000);
            var stepCount = 10;
            var stepDelay = executionTime / stepCount;

            for (int i = 0; i < stepCount; i++)
            {
                cts.Token.ThrowIfCancellationRequested();
                await Task.Delay(stepDelay, cts.Token);
            }

            // Simulate random failures (10% chance)
            if (Random.Shared.Next(100) < 10)
            {
                throw new InvalidOperationException("Simulated test failure");
            }

            // Publish test.finished
            _publisher.PublishEvent(new TestEvent
            {
                TestId = testId,
                Status = TestStatus.Finished,
                Worker = workerName,
                Timestamp = DateTime.UtcNow
            }, RabbitMqConstants.TestFinished);

            _logger.LogInformation("Test {TestId} finished successfully", testId);
        }
        catch (OperationCanceledException)
        {
            // Publish test.cancelled
            _publisher.PublishEvent(new TestEvent
            {
                TestId = testId,
                Status = TestStatus.Cancelled,
                Worker = workerName,
                Timestamp = DateTime.UtcNow
            }, RabbitMqConstants.TestCancelled);

            _logger.LogInformation("Test {TestId} was cancelled", testId);
        }
        catch (Exception ex)
        {
            // Publish test.failed
            _publisher.PublishEvent(new TestEvent
            {
                TestId = testId,
                Status = TestStatus.Failed,
                Worker = workerName,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.UtcNow
            }, RabbitMqConstants.TestFailed);

            _logger.LogError(ex, "Test {TestId} failed", testId);
        }
        finally
        {
            _cancellationManager.Unregister(testId);
        }
    }
}
