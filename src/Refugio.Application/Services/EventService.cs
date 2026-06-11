using Refugio.Application.Contracts;
using Refugio.Application.Mapping;
using Refugio.Domain.Common;
using Refugio.Domain.Entities;
using Refugio.Domain.Repositories;

namespace Refugio.Application.Services;

public interface IEventService
{
    Task<List<ShelterEventDto>> GetEventsAsync(DateTime? from = null, DateTime? to = null);
    Task<ShelterEventDto?> GetEventAsync(int id);
    Task<ShelterEventDto> ScheduleAsync(CreateEventRequest request);
    Task<ShelterEventDto?> UpdateAsync(UpdateEventRequest request);
    Task<bool> DeleteAsync(int id);
}

public class EventService(IShelterEventRepository events, IUnitOfWork unitOfWork)
    : ShelterServiceBase(unitOfWork), IEventService
{
    public async Task<List<ShelterEventDto>> GetEventsAsync(DateTime? from = null, DateTime? to = null)
        => (await events.GetAllAsync(from, to)).Select(e => e.ToDto()).ToList();

    public async Task<ShelterEventDto?> GetEventAsync(int id)
        => (await events.GetAsync(id))?.ToDto();

    public async Task<ShelterEventDto> ScheduleAsync(CreateEventRequest request)
    {
        var shelterEvent = ShelterEvent.Schedule(
            request.Title, request.StartDateTime, request.EndDateTime,
            request.Location, request.Description, request.EventType, request.AssignedVolunteers);
        events.Add(shelterEvent);
        await UnitOfWork.SaveChangesAsync();
        return shelterEvent.ToDto();
    }

    public async Task<ShelterEventDto?> UpdateAsync(UpdateEventRequest request)
    {
        var shelterEvent = await events.GetAsync(request.Id);
        if (shelterEvent is null) return null;
        shelterEvent.Update(
            request.Title, request.StartDateTime, request.EndDateTime,
            request.Location, request.Description, request.EventType, request.AssignedVolunteers);
        await UnitOfWork.SaveChangesAsync();
        return shelterEvent.ToDto();
    }

    public Task<bool> DeleteAsync(int id) => SoftDeleteAsync(() => events.GetAsync(id), events.Remove);
}
