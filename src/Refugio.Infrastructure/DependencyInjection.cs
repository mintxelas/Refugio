using Microsoft.Extensions.DependencyInjection;
using Refugio.Application.Abstractions;
using Refugio.Application.Queries;
using Refugio.Domain.Common;
using Refugio.Domain.Repositories;
using Refugio.Infrastructure.Data;
using Refugio.Infrastructure.Email;
using Refugio.Infrastructure.Queries;
using Refugio.Infrastructure.Repositories;

namespace Refugio.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers repositories, unit of work, read queries and adapters.
    /// The ShelterDbContext registration stays in the host so tests can swap the provider.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddScoped<IDogRepository, DogRepository>();
        services.AddScoped<IAdoptionRepository, AdoptionRepository>();
        services.AddScoped<IVolunteerRepository, VolunteerRepository>();
        services.AddScoped<IDonationRepository, DonationRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<IGoalRepository, GoalRepository>();
        services.AddScoped<IShelterTaskRepository, ShelterTaskRepository>();
        services.AddScoped<IShelterEventRepository, ShelterEventRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();

        services.AddScoped<IDashboardQueries, DashboardQueries>();
        services.AddScoped<IFinanceQueries, FinanceQueries>();
        services.AddScoped<IAdoptionQueries, AdoptionQueries>();
        services.AddScoped<IVolunteerQueries, VolunteerQueries>();
        services.AddScoped<IMedicalQueries, MedicalQueries>();

        services.AddSingleton<IShelterEmailSender, NoOpEmailSender>();

        return services;
    }
}
