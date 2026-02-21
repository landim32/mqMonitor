using AutoMapper;
using MqMonitor.Domain.Entities;
using MqMonitor.Domain.Entities.Interfaces;
using MqMonitor.DTO;

namespace MqMonitor.Infra.Mapping.Profiles;

public class ProcessExecutionDtoProfile : Profile
{
    public ProcessExecutionDtoProfile()
    {
        CreateMap<ProcessExecutionModel, ProcessExecutionInfo>();
        CreateMap<IProcessExecutionModel, ProcessExecutionInfo>();
    }
}
