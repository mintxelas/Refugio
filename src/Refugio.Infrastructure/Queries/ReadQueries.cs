using Microsoft.EntityFrameworkCore;
using Refugio.Application.Contracts;
using Refugio.Application.Queries;
using Refugio.Domain.Entities;
using Refugio.Domain.Helpers;
using Refugio.Infrastructure.Data;

namespace Refugio.Infrastructure.Queries;

public class DashboardQueries(ShelterDbContext db) : IDashboardQueries
{
    public async Task<DashboardStats> GetStatsAsync()
    {
        var totalDogs = await db.Dogs.CountAsync();
        var adoptionsThisWeek = await db.Adoptions
            .Where(a => a.Status == AdoptionStatus.Finalized && a.UpdatedAt >= DateTime.UtcNow.AddDays(-7))
            .CountAsync();
        var urgentMeds = await db.Medications
            .Where(m => m.IsActive && m.EndDate <= DateTime.UtcNow.AddDays(3))
            .CountAsync();
        var totalDonations = await db.Donations.SumAsync(d => (decimal?)d.Amount) ?? 0;
        var donationGoal = await db.Goals.SumAsync(g => (decimal?)g.TargetAmount) ?? 0;
        return new DashboardStats(totalDogs, adoptionsThisWeek, urgentMeds, totalDonations, donationGoal);
    }
}

public class FinanceQueries(ShelterDbContext db) : IFinanceQueries
{
    public async Task<FinanceSummary> GetSummaryAsync(int year)
    {
        var donationsByMonth = await db.Donations
            .Where(d => d.Date.Year == year)
            .GroupBy(d => d.Date.Month)
            .Select(g => new { Month = g.Key, Total = g.Sum(d => d.Amount) })
            .ToDictionaryAsync(g => g.Month, g => g.Total);

        var expensesByMonth = await db.Expenses
            .Where(e => e.Date.Year == year)
            .GroupBy(e => e.Date.Month)
            .Select(g => new { Month = g.Key, Total = g.Sum(e => e.Amount) })
            .ToDictionaryAsync(g => g.Month, g => g.Total);

        var monthly = Enumerable.Range(1, 12).Select(m => new MonthSummary(
            m,
            donationsByMonth.GetValueOrDefault(m),
            expensesByMonth.GetValueOrDefault(m))).ToList();

        return new FinanceSummary(
            donationsByMonth.Values.Sum(),
            expensesByMonth.Values.Sum(),
            monthly);
    }
}

public class AdoptionQueries(ShelterDbContext db) : IAdoptionQueries
{
    public async Task<AdoptionConversionStats> GetConversionStatsAsync(int year)
    {
        var applied = await db.Adoptions
            .Where(a => a.CreatedAt.Year == year)
            .GroupBy(a => a.CreatedAt.Month)
            .Select(g => new { Month = g.Key, Count = g.Count() })
            .ToListAsync();

        var finalized = await db.Adoptions
            .Where(a => a.Status == AdoptionStatus.Finalized && a.UpdatedAt != null && a.UpdatedAt.Value.Year == year)
            .GroupBy(a => a.UpdatedAt!.Value.Month)
            .Select(g => new { Month = g.Key, Count = g.Count() })
            .ToListAsync();

        var monthly = Enumerable.Range(1, 12)
            .Select(m => new MonthlyConversionData(
                m,
                applied.FirstOrDefault(x => x.Month == m)?.Count ?? 0,
                finalized.FirstOrDefault(x => x.Month == m)?.Count ?? 0))
            .ToList();

        return new AdoptionConversionStats(monthly, applied.Sum(x => x.Count), finalized.Sum(x => x.Count));
    }

    public async Task<ShelterStayStats> GetShelterStayStatsAsync()
    {
        var data = await db.Adoptions
            .Where(a => a.Status == AdoptionStatus.Finalized && a.UpdatedAt != null)
            .Join(db.Dogs, a => a.DogId, d => d.Id,
                (a, d) => new { d.Breed, d.ArrivalDate, FinalizedAt = a.UpdatedAt!.Value })
            .ToListAsync();

        var byBreed = data
            .GroupBy(x => x.Breed)
            .Select(g => new BreedStayData(
                g.Key,
                g.Average(x => (x.FinalizedAt - x.ArrivalDate).TotalDays),
                g.Count()))
            .OrderByDescending(s => s.Count)
            .ToList();

        return new ShelterStayStats(byBreed);
    }
}

public class VolunteerQueries(ShelterDbContext db) : IVolunteerQueries
{
    public async Task<VolunteerCounts> GetCountsAsync()
    {
        var total = await db.Volunteers.CountAsync();
        var active = await db.Volunteers.CountAsync(v => v.Status == VolunteerStatus.Active);
        var pending = await db.Volunteers.CountAsync(v => v.Status == VolunteerStatus.Pending);
        return new VolunteerCounts(total, active, pending);
    }

    public Task<List<string>> GetManagerEmailsAsync()
        => db.Volunteers
            .Where(v => v.Role == Roles.Manager && v.CanLogin && v.Email != null)
            .Select(v => v.Email!)
            .ToListAsync();
}

public class MedicalQueries(ShelterDbContext db) : IMedicalQueries
{
    public Task<List<UpcomingVisit>> GetUpcomingVisitsAsync(int daysAhead)
    {
        var today = DateTime.UtcNow.Date;
        var horizon = today.AddDays(daysAhead);
        return db.MedicalRecords
            .Where(r => r.NextVisitDate.HasValue
                && r.NextVisitDate.Value.Date >= today
                && r.NextVisitDate.Value.Date <= horizon)
            .Select(r => new UpcomingVisit(r.Dog.Name, r.NextVisitDate!.Value, r.VetName, r.Diagnosis))
            .ToListAsync();
    }
}
