using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MqMonitor.Domain.Services.Interfaces;
using MqMonitor.DTO;
using MqMonitor.Infra.Configuration;

namespace MqMonitor.API.Controllers;

[ApiController]
[Route("api/queues")]
public class QueuesController : ControllerBase
{
    private readonly IRabbitMqManagementService _managementService;
    private readonly PipelineSettings _pipelineSettings;
    private readonly ILogger<QueuesController> _logger;

    public QueuesController(
        IRabbitMqManagementService managementService,
        IOptions<PipelineSettings> pipelineSettings,
        ILogger<QueuesController> logger)
    {
        _managementService = managementService;
        _pipelineSettings = pipelineSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Get status of all queues
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<QueueStatusInfo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var queues = await _managementService.GetAllQueueStatusAsync();
        return Ok(queues);
    }

    /// <summary>
    /// Get status of a specific queue
    /// </summary>
    [HttpGet("{name}")]
    [ProducesResponseType(typeof(QueueStatusInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByName(string name)
    {
        var queue = await _managementService.GetQueueStatusAsync(name);

        if (queue == null)
            return NotFound(new { message = $"Queue '{name}' not found" });

        return Ok(queue);
    }

    /// <summary>
    /// Get pipeline overview with all stage queues and system queues
    /// </summary>
    [HttpGet("pipeline")]
    [ProducesResponseType(typeof(PipelineStatusInfo), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPipelineStatus()
    {
        var pipeline = await _managementService.GetPipelineStatusAsync();
        return Ok(pipeline);
    }

    /// <summary>
    /// Get configured pipeline stages with queue names (including DLQ and retry)
    /// </summary>
    [HttpGet("stages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetConfiguredStages()
    {
        var stages = _pipelineSettings.Stages.Select(s => new
        {
            s.Name,
            s.DisplayName,
            s.QueueName,
            s.RoutingKey,
            DlqName = s.DlqName ?? string.Empty,
            RetryQueueName = $"{s.QueueName}.retry"
        });

        return Ok(stages);
    }
}
