using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MqMonitor.Domain.Entities;
using MqMonitor.Domain.Entities.Interfaces;
using MqMonitor.Infra.Context;
using MqMonitor.Infra.Interfaces.Repository;

namespace MqMonitor.Infra.Repository;

public class ProcessExecutionRepository : IProcessExecutionRepository<IProcessExecutionModel>
{
    private readonly MonitorDbContext _context;
    private readonly IMapper _mapper;

    public ProcessExecutionRepository(MonitorDbContext context, IMapper mapper)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<IEnumerable<IProcessExecutionModel>> GetAllAsync()
    {
        var entities = await _context.ProcessExecutions
            .AsNoTracking()
            .OrderByDescending(e => e.UpdatedAt)
            .ToListAsync();

        return _mapper.Map<IEnumerable<ProcessExecutionModel>>(entities);
    }

    public async Task<IProcessExecutionModel?> GetByIdAsync(string processId)
    {
        var entity = await _context.ProcessExecutions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ProcessId == processId);

        if (entity == null) return null;
        return _mapper.Map<ProcessExecutionModel>(entity);
    }

    public async Task<IProcessExecutionModel> InsertAsync(IProcessExecutionModel model)
    {
        var entity = _mapper.Map<ProcessExecution>(model);
        _context.ProcessExecutions.Add(entity);
        await _context.SaveChangesAsync();
        return _mapper.Map<ProcessExecutionModel>(entity);
    }

    public async Task<IProcessExecutionModel> UpdateAsync(IProcessExecutionModel model)
    {
        var existing = await _context.ProcessExecutions
            .FirstOrDefaultAsync(e => e.ProcessId == model.ProcessId)
            ?? throw new KeyNotFoundException($"ProcessExecution with ID {model.ProcessId} not found.");

        existing.Status = model.Status;
        existing.Worker = model.Worker;
        existing.StartedAt = model.StartedAt;
        existing.FinishedAt = model.FinishedAt;
        existing.UpdatedAt = model.UpdatedAt;
        existing.ErrorMessage = model.ErrorMessage;
        existing.Message = model.Message;
        existing.CurrentStage = model.CurrentStage;
        existing.Priority = model.Priority;
        existing.SagaStatus = model.SagaStatus;

        await _context.SaveChangesAsync();
        return _mapper.Map<ProcessExecutionModel>(existing);
    }

    public async Task<IEnumerable<IProcessExecutionModel>> GetByStageAsync(string stageName)
    {
        var entities = await _context.ProcessExecutions
            .AsNoTracking()
            .Where(e => e.CurrentStage == stageName)
            .OrderByDescending(e => e.UpdatedAt)
            .ToListAsync();

        return _mapper.Map<IEnumerable<ProcessExecutionModel>>(entities);
    }

    public async Task<IEnumerable<IProcessExecutionModel>> GetByStatusAsync(string status)
    {
        var entities = await _context.ProcessExecutions
            .AsNoTracking()
            .Where(e => e.Status == status)
            .OrderByDescending(e => e.UpdatedAt)
            .ToListAsync();

        return _mapper.Map<IEnumerable<ProcessExecutionModel>>(entities);
    }
}
