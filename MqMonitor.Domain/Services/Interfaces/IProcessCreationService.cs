using MqMonitor.DTO;

namespace MqMonitor.Domain.Services.Interfaces;

public interface IProcessCreationService
{
    CreateProcessResponse CreateProcess(CreateProcessRequest request);
    List<string> GetAvailableStages();
}
