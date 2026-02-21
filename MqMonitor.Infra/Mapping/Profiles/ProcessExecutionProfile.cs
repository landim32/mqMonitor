using AutoMapper;
using MqMonitor.Domain.Entities;
using MqMonitor.Infra.Context;

namespace MqMonitor.Infra.Mapping.Profiles;

public class ProcessExecutionProfile : Profile
{
    public ProcessExecutionProfile()
    {
        CreateMap<ProcessExecution, ProcessExecutionModel>()
            .ConstructUsing(src => ProcessExecutionModel.Reconstruct(
                src.ProcessId, src.Status, src.Worker,
                src.StartedAt, src.FinishedAt,
                src.UpdatedAt, src.ErrorMessage,
                src.Message, src.CurrentStage,
                src.Priority, src.SagaStatus));

        CreateMap<ProcessExecutionModel, ProcessExecution>();
    }
}
