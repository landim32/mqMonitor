using MqMonitor.Domain.Entities.Interfaces;

namespace MqMonitor.Infra.Interfaces.Repository;

public interface IProcessExecutionRepository<TModel> where TModel : IProcessExecutionModel
{
    Task<IEnumerable<TModel>> GetAllAsync();
    Task<TModel?> GetByIdAsync(string processId);
    Task<TModel> InsertAsync(TModel model);
    Task<TModel> UpdateAsync(TModel model);
    Task<IEnumerable<TModel>> GetByStageAsync(string stageName);
    Task<IEnumerable<TModel>> GetByStatusAsync(string status);
}
