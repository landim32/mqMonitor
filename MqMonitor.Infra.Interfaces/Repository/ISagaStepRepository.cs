using MqMonitor.Domain.Entities.Interfaces;

namespace MqMonitor.Infra.Interfaces.Repository;

public interface ISagaStepRepository<TModel> where TModel : ISagaStepModel
{
    Task<IEnumerable<TModel>> GetByProcessIdAsync(string processId);
    Task<TModel> InsertAsync(TModel model);
    Task<TModel> UpdateAsync(TModel model);
    Task<TModel?> GetLastStepAsync(string processId);
}
