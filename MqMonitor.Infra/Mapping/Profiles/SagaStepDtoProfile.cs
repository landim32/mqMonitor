using AutoMapper;
using MqMonitor.Domain.Entities;
using MqMonitor.Domain.Entities.Interfaces;
using MqMonitor.DTO;

namespace MqMonitor.Infra.Mapping.Profiles;

public class SagaStepDtoProfile : Profile
{
    public SagaStepDtoProfile()
    {
        CreateMap<SagaStepModel, SagaStepInfo>();
        CreateMap<ISagaStepModel, SagaStepInfo>();
    }
}
