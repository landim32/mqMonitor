using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MqMonitor.Domain.Services.Interfaces;
using MqMonitor.DTO;
using MqMonitor.Infra.Configuration;

namespace MqMonitor.Infra.Services;

public class RabbitMqManagementService : IRabbitMqManagementService
{
    private readonly HttpClient _httpClient;
    private readonly RabbitMqSettings _rabbitSettings;
    private readonly PipelineSettings _pipelineSettings;
    private readonly ILogger<RabbitMqManagementService> _logger;

    public RabbitMqManagementService(
        HttpClient httpClient,
        IOptions<RabbitMqSettings> rabbitSettings,
        IOptions<PipelineSettings> pipelineSettings,
        ILogger<RabbitMqManagementService> logger)
    {
        _httpClient = httpClient;
        _rabbitSettings = rabbitSettings.Value;
        _pipelineSettings = pipelineSettings.Value;
        _logger = logger;

        // Configure HttpClient for RabbitMQ Management API
        var baseUrl = $"http://{_rabbitSettings.HostName}:{_rabbitSettings.ManagementPort}";
        _httpClient.BaseAddress = new Uri(baseUrl);

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_rabbitSettings.UserName}:{_rabbitSettings.Password}"));
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);
    }

    public async Task<List<QueueStatusInfo>> GetAllQueueStatusAsync()
    {
        try
        {
            var vhost = Uri.EscapeDataString(_rabbitSettings.VirtualHost);
            var response = await _httpClient.GetAsync($"/api/queues/{vhost}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var queues = JsonSerializer.Deserialize<List<JsonElement>>(json) ?? new();

            return queues.Select(MapToQueueStatus).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch queue status from RabbitMQ Management API");
            return new List<QueueStatusInfo>();
        }
    }

    public async Task<QueueStatusInfo?> GetQueueStatusAsync(string queueName)
    {
        try
        {
            var vhost = Uri.EscapeDataString(_rabbitSettings.VirtualHost);
            var encodedName = Uri.EscapeDataString(queueName);
            var response = await _httpClient.GetAsync($"/api/queues/{vhost}/{encodedName}");

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var queue = JsonSerializer.Deserialize<JsonElement>(json);
            return MapToQueueStatus(queue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch status for queue {QueueName}", queueName);
            return null;
        }
    }

    public async Task<PipelineStatusInfo> GetPipelineStatusAsync()
    {
        var allQueues = await GetAllQueueStatusAsync();

        var stageQueueNames = _pipelineSettings.Stages
            .Select(s => s.QueueName)
            .ToHashSet();

        var stageQueues = allQueues
            .Where(q => stageQueueNames.Contains(q.QueueName))
            .ToList();

        // Enrich with stage metadata
        foreach (var queue in stageQueues)
        {
            var stage = _pipelineSettings.Stages
                .FirstOrDefault(s => s.QueueName == queue.QueueName);
            if (stage != null)
            {
                queue.StageName = stage.Name;
                queue.DisplayName = stage.DisplayName;
            }
        }

        var systemQueues = allQueues
            .Where(q => !stageQueueNames.Contains(q.QueueName))
            .ToList();

        return new PipelineStatusInfo
        {
            Stages = stageQueues,
            SystemQueues = systemQueues,
            TotalMessages = allQueues.Sum(q => q.MessageCount),
            TotalConsumers = allQueues.Sum(q => q.ConsumerCount)
        };
    }

    private QueueStatusInfo MapToQueueStatus(JsonElement queue)
    {
        var name = queue.GetProperty("name").GetString() ?? string.Empty;

        return new QueueStatusInfo
        {
            QueueName = name,
            MessageCount = GetIntProperty(queue, "messages"),
            ConsumerCount = GetIntProperty(queue, "consumers"),
            PublishRate = GetMessageRate(queue, "message_stats", "publish_details"),
            DeliverRate = GetMessageRate(queue, "message_stats", "deliver_get_details"),
            AckRate = GetMessageRate(queue, "message_stats", "ack_details"),
            State = queue.TryGetProperty("state", out var state) ? state.GetString() ?? "unknown" : "unknown"
        };
    }

    private static int GetIntProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number)
            return prop.GetInt32();
        return 0;
    }

    private static double GetMessageRate(JsonElement element, string statsProperty, string detailsProperty)
    {
        if (element.TryGetProperty(statsProperty, out var stats) &&
            stats.TryGetProperty(detailsProperty, out var details) &&
            details.TryGetProperty("rate", out var rate) &&
            rate.ValueKind == JsonValueKind.Number)
        {
            return rate.GetDouble();
        }
        return 0;
    }
}
