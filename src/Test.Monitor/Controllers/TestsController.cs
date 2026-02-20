using Microsoft.AspNetCore.Mvc;
using Test.Contracts.Commands;
using Test.Contracts.Models;
using Test.Infrastructure.RabbitMq;
using Test.Monitor.Services;

namespace Test.Monitor.Controllers;

[ApiController]
[Route("api/tests")]
public class TestsController : ControllerBase
{
    private readonly TestQueryService _queryService;
    private readonly RabbitMqPublisher _publisher;
    private readonly ILogger<TestsController> _logger;

    public TestsController(
        TestQueryService queryService,
        RabbitMqPublisher publisher,
        ILogger<TestsController> logger)
    {
        _queryService = queryService;
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>
    /// List all test executions
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<TestExecution>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var executions = await _queryService.GetAllExecutions();
        return Ok(executions);
    }

    /// <summary>
    /// Get test execution details by ID
    /// </summary>
    [HttpGet("{testId}")]
    [ProducesResponseType(typeof(TestExecution), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string testId)
    {
        var execution = await _queryService.GetExecution(testId);

        if (execution == null)
            return NotFound(new { message = $"Test '{testId}' not found" });

        return Ok(execution);
    }

    /// <summary>
    /// Get event history for a specific test
    /// </summary>
    [HttpGet("{testId}/events")]
    [ProducesResponseType(typeof(List<EventLog>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents(string testId)
    {
        var events = await _queryService.GetEventsByTestId(testId);
        return Ok(events);
    }

    /// <summary>
    /// Cancel a test execution
    /// </summary>
    [HttpPost("{testId}/cancel")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(string testId)
    {
        var execution = await _queryService.GetExecution(testId);

        if (execution == null)
            return NotFound(new { message = $"Test '{testId}' not found" });

        if (execution.Status is TestStatus.Finished or TestStatus.Failed or TestStatus.Cancelled)
            return Conflict(new { message = $"Test '{testId}' is already in terminal state: {execution.Status}" });

        var command = new CancelTestCommand
        {
            TestId = testId,
            RequestedAt = DateTime.UtcNow
        };

        _publisher.PublishCancelCommand(command);

        _logger.LogInformation(
            "Cancel command {CommandId} published for test {TestId}",
            command.CommandId, testId);

        return Accepted(new
        {
            message = $"Cancel command sent for test '{testId}'",
            commandId = command.CommandId
        });
    }

    /// <summary>
    /// Get execution metrics
    /// </summary>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(TestMetrics), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetrics()
    {
        var metrics = await _queryService.GetMetrics();
        return Ok(metrics);
    }
}
