using AutoMapper;
using MqMonitor.Domain.Entities;
using MqMonitor.Domain.Entities.Interfaces;
using MqMonitor.DTO;

namespace MqMonitor.Infra.Mapping.Profiles;

public class EventLogDtoProfile : Profile
{
    public EventLogDtoProfile()
    {
        CreateMap<EventLogModel, EventLogInfo>();
        CreateMap<IEventLogModel, EventLogInfo>();
    }
}
