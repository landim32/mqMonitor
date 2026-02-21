using Microsoft.AspNetCore.Mvc;
using MqMonitor.Domain.Messaging.Interfaces;
using MqMonitor.Domain.Services.Interfaces;
using MqMonitor.DTO;
using MqMonitor.Infra.Configuration;
using MqMonitor.Infra.Messaging.Contracts;

namespace MqMonitor.API.Controllers;

[ApiController]
[Route("api/processes")]
public class ProcessesController : ControllerBase
{
    private readonly IProcessQueryService _queryService;
    private readonly IProcessCreationService _creationService;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<ProcessesController> _logger;

    public ProcessesController(
        IProcessQueryService queryService,
        IProcessCreationService creationService,
        IMessagePublisher publisher,
        ILogger<ProcessesController> logger)
    {
        _queryService = queryService;
        _creationService = creationService;
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>
    /// List all process executions with optional filters by stage or status
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ProcessExecutionInfo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? stage = null,
        [FromQuery] string? status = null)
    {
        List<ProcessExecutionInfo> executions;

        if (!string.IsNullOrEmpty(stage))
        {
            executions = await _queryService.GetExecutionsByStageAsync(stage);
        }
        else if (!string.IsNullOrEmpty(status))
        {
            executions = await _queryService.GetExecutionsByStatusAsync(status);
        }
        else
        {
            executions = await _queryService.GetAllExecutionsAsync();
        }

        return Ok(executions);
    }

    /// <summary>
    /// Create a new process and send it to a pipeline stage
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateProcessResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Create([FromBody] CreateProcessRequest request)
    {
        try
        {
            var response = _creationService.CreateProcess(request);

            _logger.LogInformation(
                "Process {ProcessId} created via API for stage '{Stage}'",
                response.ProcessId, response.StageName);

            return CreatedAtAction(
                nameof(GetById),
                new { processId = response.ProcessId },
                response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get process execution details by ID
    /// </summary>
    [HttpGet("{processId}")]
    [ProducesResponseType(typeof(ProcessExecutionInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string processId)
    {
        var execution = await _queryService.GetExecutionAsync(processId);

        if (execution == null)
            return NotFound(new { message = $"Process '{processId}' not found" });

        return Ok(execution);
    }

    /// <summary>
    /// Get event history for a specific process
    /// </summary>
    [HttpGet("{processId}/events")]
    [ProducesResponseType(typeof(List<EventLogInfo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents(string processId)
    {
        var events = await _queryService.GetEventsByProcessIdAsync(processId);
        return Ok(events);
    }

    /// <summary>
    /// Get saga steps (timeline) for a specific process — shows where the process has been
    /// </summary>
    [HttpGet("{processId}/saga")]
    [ProducesResponseType(typeof(List<SagaStepInfo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSagaSteps(string processId)
    {
        var steps = await _queryService.GetSagaStepsAsync(processId);
        return Ok(steps);
    }

    /// <summary>
    /// Update process priority
    /// </summary>
    [HttpPut("{processId}/priority")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePriority(
        string processId, [FromBody] ChangePriorityRequest request)
    {
        var updated = await _queryService.UpdatePriorityAsync(processId, request.Priority);

        if (!updated)
            return NotFound(new { message = $"Process '{processId}' not found" });

        _logger.LogInformation(
            "Priority updated for process {ProcessId} to {Priority}",
            processId, request.Priority);

        return Ok(new
        {
            message = $"Priority updated to {request.Priority}",
            processId,
            priority = request.Priority
        });
    }

    /// <summary>
    /// Cancel a process execution
    /// </summary>
    [HttpPost("{processId}/cancel")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(string processId)
    {
        var execution = await _queryService.GetExecutionAsync(processId);

        if (execution == null)
            return NotFound(new { message = $"Process '{processId}' not found" });

        if (execution.Status is "FINISHED" or "FAILED" or "CANCELLED" or "COMPENSATED")
            return Conflict(new { message = $"Process '{processId}' is already in terminal state: {execution.Status}" });

        var command = new CancelProcessCommand
        {
            ProcessId = processId,
            RequestedAt = DateTime.UtcNow
        };

        _publisher.PublishCommand(command, RabbitMqConstants.CommandsExchange, RabbitMqConstants.CancelProcess);

        _logger.LogInformation(
            "Cancel command {CommandId} published for process {ProcessId}",
            command.CommandId, processId);

        return Accepted(new
        {
            message = $"Cancel command sent for process '{processId}'",
            commandId = command.CommandId
        });
    }

    /// <summary>
    /// Get execution metrics with stage breakdown
    /// </summary>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(ProcessMetricsInfo), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetrics()
    {
        var metrics = await _queryService.GetMetricsAsync();
        return Ok(metrics);
    }
}
