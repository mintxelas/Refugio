using Microsoft.Extensions.DependencyInjection;
using Refugio.Application.Abstractions;
using Refugio.Application.Events;
using Refugio.Application.Services;
using Refugio.Domain.Events;

namespace Refugio.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IDogService, DogService>();
        services.AddScoped<IAdoptionService, AdoptionService>();
        services.AddScoped<IVolunteerService, VolunteerService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IFinanceService, FinanceService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<VetAppointmentNotifier>();

        services.AddScoped<IDomainEventHandler<AdoptionStatusChanged>, AdoptionStatusChangedHandler>();

        return services;
    }
}
