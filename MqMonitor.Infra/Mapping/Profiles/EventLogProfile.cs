using AutoMapper;
using MqMonitor.Domain.Entities;
using MqMonitor.Infra.Context;

namespace MqMonitor.Infra.Mapping.Profiles;

public class EventLogProfile : Profile
{
    public EventLogProfile()
    {
        CreateMap<EventLog, EventLogModel>()
            .ConstructUsing(src => EventLogModel.Reconstruct(
                src.EventId, src.ProcessId, src.Type, src.Payload, src.Timestamp));

        CreateMap<EventLogModel, EventLog>();
    }
}
