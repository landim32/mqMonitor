using AutoMapper;
using MqMonitor.Domain.Entities;
using MqMonitor.Domain.Entities.Interfaces;
using MqMonitor.Domain.Services.Interfaces;
using MqMonitor.DTO;
using MqMonitor.Infra.Interfaces.Repository;

namespace MqMonitor.Infra.Services;

public class ProcessQueryService : IProcessQueryService
{
    private readonly IProcessExecutionRepository<IProcessExecutionModel> _executionRepo;
    private readonly IEventLogRepository<IEventLogModel> _eventLogRepo;
    private readonly ISagaStepRepository<ISagaStepModel> _sagaStepRepo;
    private readonly IMapper _mapper;

    public ProcessQueryService(
        IProcessExecutionRepository<IProcessExecutionModel> executionRepo,
        IEventLogRepository<IEventLogModel> eventLogRepo,
        ISagaStepRepository<ISagaStepModel> sagaStepRepo,
        IMapper mapper)
    {
        _executionRepo = executionRepo ?? throw new ArgumentNullException(nameof(executionRepo));
        _eventLogRepo = eventLogRepo ?? throw new ArgumentNullException(nameof(eventLogRepo));
        _sagaStepRepo = sagaStepRepo ?? throw new ArgumentNullException(nameof(sagaStepRepo));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<List<ProcessExecutionInfo>> GetAllExecutionsAsync()
    {
        var models = await _executionRepo.GetAllAsync();
        return _mapper.Map<List<ProcessExecutionInfo>>(models);
    }

    public async Task<ProcessExecutionInfo?> GetExecutionAsync(string processId)
    {
        var model = await _executionRepo.GetByIdAsync(processId);
        if (model == null) return null;
        return _mapper.Map<ProcessExecutionInfo>(model);
    }

    public async Task<List<EventLogInfo>> GetEventsByProcessIdAsync(string processId)
    {
        var models = await _eventLogRepo.GetByProcessIdAsync(processId);
        return _mapper.Map<List<EventLogInfo>>(models);
    }

    public async Task<ProcessMetricsInfo> GetMetricsAsync()
    {
        var executions = (await _executionRepo.GetAllAsync()).ToList();

        var completedWithTimes = executions
            .Where(e => e.StartedAt.HasValue && e.FinishedAt.HasValue)
            .ToList();

        var totalExecuted = executions.Count;
        var inProgress = executions.Count(e =>
            e.Status is "STARTED" or "CREATED" or "QUEUED" or "STAGE_STARTED" or "STAGE_COMPLETED");
        var failed = executions.Count(e => e.Status == "FAILED");
        var cancelled = executions.Count(e => e.Status == "CANCELLED");
        var finished = executions.Count(e => e.Status == "FINISHED");

        var averageMs = completedWithTimes.Count > 0
            ? completedWithTimes.Average(e =>
                (e.FinishedAt!.Value - e.StartedAt!.Value).TotalMilliseconds)
            : 0;

        var errorRate = totalExecuted > 0
            ? (double)failed / totalExecuted * 100
            : 0;

        // Group by current stage
        var byStage = executions
            .Where(e => !string.IsNullOrEmpty(e.CurrentStage))
            .GroupBy(e => e.CurrentStage!)
            .ToDictionary(g => g.Key, g => g.Count());

        return new ProcessMetricsInfo
        {
            TotalExecuted = totalExecuted,
            InProgress = inProgress,
            Failed = failed,
            Cancelled = cancelled,
            Finished = finished,
            AverageExecutionTimeMs = Math.Round(averageMs, 2),
            ErrorRate = Math.Round(errorRate, 2),
            ByStage = byStage
        };
    }

    public async Task<List<SagaStepInfo>> GetSagaStepsAsync(string processId)
    {
        var models = await _sagaStepRepo.GetByProcessIdAsync(processId);
        return _mapper.Map<List<SagaStepInfo>>(models);
    }

    public async Task<List<ProcessExecutionInfo>> GetExecutionsByStageAsync(string stageName)
    {
        var models = await _executionRepo.GetByStageAsync(stageName);
        return _mapper.Map<List<ProcessExecutionInfo>>(models);
    }

    public async Task<List<ProcessExecutionInfo>> GetExecutionsByStatusAsync(string status)
    {
        var models = await _executionRepo.GetByStatusAsync(status);
        return _mapper.Map<List<ProcessExecutionInfo>>(models);
    }

    public async Task<bool> UpdatePriorityAsync(string processId, int priority)
    {
        var existing = await _executionRepo.GetByIdAsync(processId);
        if (existing == null) return false;

        var mutableModel = ProcessExecutionModel.Reconstruct(
            existing.ProcessId, existing.Status, existing.Worker,
            existing.StartedAt, existing.FinishedAt,
            existing.UpdatedAt, existing.ErrorMessage,
            existing.Message, existing.CurrentStage,
            existing.Priority, existing.SagaStatus);

        mutableModel.UpdatePriority(priority);
        await _executionRepo.UpdateAsync(mutableModel);
        return true;
    }
}
