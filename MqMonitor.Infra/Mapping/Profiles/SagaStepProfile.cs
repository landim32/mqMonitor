using AutoMapper;
using MqMonitor.Domain.Entities;
using MqMonitor.Infra.Context;

namespace MqMonitor.Infra.Mapping.Profiles;

public class SagaStepProfile : Profile
{
    public SagaStepProfile()
    {
        CreateMap<SagaStep, SagaStepModel>()
            .ConstructUsing(src => SagaStepModel.Reconstruct(
                src.StepId, src.ProcessId, src.StageName,
                src.Status, src.Worker, src.StartedAt,
                src.CompletedAt, src.ErrorMessage, src.StepOrder));

        CreateMap<SagaStepModel, SagaStep>();
    }
}
