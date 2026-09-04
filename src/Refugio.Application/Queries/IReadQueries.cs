using Refugio.Application.Contracts;

namespace Refugio.Application.Queries;

// CQRS-lite read models: aggregations that cut across aggregates. Implemented in
// Infrastructure with direct LINQ; endpoints inject them straight (no service ceremony).

public interface IDashboardQueries
{
    Task<DashboardStats> GetStatsAsync();

    /// <summary>Dogs with an active medication ending within 3 days.</summary>
    Task<List<UrgentMedicationDto>> GetUrgentMedicationsAsync();
}

public interface IFinanceQueries
{
    Task<FinanceSummary> GetSummaryAsync(int year);
}

public interface IAdoptionQueries
{
    Task<AdoptionConversionStats> GetConversionStatsAsync(int year);
    Task<ShelterStayStats> GetShelterStayStatsAsync();
}

public interface IVolunteerQueries
{
    Task<VolunteerCounts> GetCountsAsync();
    Task<List<string>> GetManagerEmailsAsync();
}

public interface IMedicalQueries
{
    /// <summary>Vet visits scheduled from today up to <paramref name="daysAhead"/> days out.</summary>
    Task<List<UpcomingVisit>> GetUpcomingVisitsAsync(int daysAhead);
}
