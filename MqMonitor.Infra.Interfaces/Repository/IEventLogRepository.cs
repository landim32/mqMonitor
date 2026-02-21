using MqMonitor.Domain.Entities.Interfaces;

namespace MqMonitor.Infra.Interfaces.Repository;

public interface IEventLogRepository<TModel> where TModel : IEventLogModel
{
    Task<bool> ExistsAsync(string eventId);
    Task<TModel> InsertAsync(TModel model);
    Task<IEnumerable<TModel>> GetByProcessIdAsync(string processId);
}
