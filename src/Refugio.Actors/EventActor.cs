using Microsoft.Extensions.DependencyInjection;
using Refugio.Actors.Messages;
using Refugio.Application.Contracts;
using Refugio.Application.Services;

namespace Refugio.Actors;

/// <summary>Routes calendar/event operations to IEventService.</summary>
public sealed class EventActor : ShelterActorBase<IEventService>
{
    public EventActor(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
        Query<GetEvents>(async (s, m) => await s.GetEventsAsync(m.From, m.To));
        Query<GetEvent>(async (s, m) => await s.GetEventAsync(m.Id));
        Command<CreateEventRequest>(async (s, m) => await s.ScheduleAsync(m));
        Command<UpdateEventRequest>(async (s, m) => await s.UpdateAsync(m));
        Command<DeleteEvent>(async (s, m) => await s.DeleteAsync(m.Id));
    }
}
