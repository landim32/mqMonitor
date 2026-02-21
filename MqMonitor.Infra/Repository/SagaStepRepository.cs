using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MqMonitor.Domain.Entities;
using MqMonitor.Domain.Entities.Interfaces;
using MqMonitor.Infra.Context;
using MqMonitor.Infra.Interfaces.Repository;

namespace MqMonitor.Infra.Repository;

public class SagaStepRepository : ISagaStepRepository<ISagaStepModel>
{
    private readonly MonitorDbContext _context;
    private readonly IMapper _mapper;

    public SagaStepRepository(MonitorDbContext context, IMapper mapper)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<IEnumerable<ISagaStepModel>> GetByProcessIdAsync(string processId)
    {
        var entities = await _context.SagaSteps
            .AsNoTracking()
            .Where(e => e.ProcessId == processId)
            .OrderBy(e => e.StepOrder)
            .ToListAsync();

        return _mapper.Map<IEnumerable<SagaStepModel>>(entities);
    }

    public async Task<ISagaStepModel> InsertAsync(ISagaStepModel model)
    {
        var entity = _mapper.Map<SagaStep>(model);
        _context.SagaSteps.Add(entity);
        await _context.SaveChangesAsync();
        return _mapper.Map<SagaStepModel>(entity);
    }

    public async Task<ISagaStepModel> UpdateAsync(ISagaStepModel model)
    {
        var existing = await _context.SagaSteps
            .FirstOrDefaultAsync(e => e.StepId == model.StepId)
            ?? throw new KeyNotFoundException($"SagaStep with ID {model.StepId} not found.");

        existing.Status = model.Status;
        existing.CompletedAt = model.CompletedAt;
        existing.ErrorMessage = model.ErrorMessage;

        await _context.SaveChangesAsync();
        return _mapper.Map<SagaStepModel>(existing);
    }

    public async Task<ISagaStepModel?> GetLastStepAsync(string processId)
    {
        var entity = await _context.SagaSteps
            .AsNoTracking()
            .Where(e => e.ProcessId == processId)
            .OrderByDescending(e => e.StepOrder)
            .FirstOrDefaultAsync();

        if (entity == null) return null;
        return _mapper.Map<SagaStepModel>(entity);
    }
}
