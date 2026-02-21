using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MqMonitor.Domain.Entities;
using MqMonitor.Domain.Entities.Interfaces;
using MqMonitor.Infra.Context;
using MqMonitor.Infra.Interfaces.Repository;

namespace MqMonitor.Infra.Repository;

public class EventLogRepository : IEventLogRepository<IEventLogModel>
{
    private readonly MonitorDbContext _context;
    private readonly IMapper _mapper;

    public EventLogRepository(MonitorDbContext context, IMapper mapper)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<bool> ExistsAsync(string eventId)
    {
        return await _context.EventLogs.AnyAsync(e => e.EventId == eventId);
    }

    public async Task<IEventLogModel> InsertAsync(IEventLogModel model)
    {
        var entity = _mapper.Map<EventLog>(model);
        _context.EventLogs.Add(entity);
        await _context.SaveChangesAsync();
        return _mapper.Map<EventLogModel>(entity);
    }

    public async Task<IEnumerable<IEventLogModel>> GetByProcessIdAsync(string processId)
    {
        var entities = await _context.EventLogs
            .AsNoTracking()
            .Where(e => e.ProcessId == processId)
            .OrderBy(e => e.Timestamp)
            .ToListAsync();

        return _mapper.Map<IEnumerable<EventLogModel>>(entities);
    }
}
