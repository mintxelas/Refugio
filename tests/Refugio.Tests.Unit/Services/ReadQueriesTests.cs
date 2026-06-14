using Refugio.Application.Queries;
using Refugio.Domain.Entities;
using Refugio.Tests.Helpers;

namespace Refugio.Tests.Services;

public class DashboardQueriesTests : ServiceTestBase
{
    private Task<T> Query<T>(Func<IDashboardQueries, Task<T>> action) => WithServiceAsync(action);

    [Fact]
    public async Task GetStats_ReturnsZeroes_WhenEmpty()
    {
        var stats = await Query(q => q.GetStatsAsync());
        Assert.Equal(0, stats.TotalDogs);
        Assert.Equal(0, stats.NewAdoptions);
        Assert.Equal(0m, stats.TotalDonations);
        Assert.Equal(0m, stats.DonationGoal);
    }

    [Fact]
    public async Task GetStats_CountsDogs()
    {
        await SeedAsync(db => { var d = Dog.CheckIn("A", "Lab", 12, "M", 10m); db.Dogs.Add(d); return d; });
        var stats = await Query(q => q.GetStatsAsync());
        Assert.Equal(1, stats.TotalDogs);
    }

    [Fact]
    public async Task GetStats_DonationGoal_IsSumOfGoalTargets()
    {
        await SeedAsync(db =>
        {
            db.Goals.Add(Goal.Create("Van", null, 10000m, 0m));
            db.Goals.Add(Goal.Create("Heating", null, 2000m, 500m));
            return db;
        });
        var stats = await Query(q => q.GetStatsAsync());
        Assert.Equal(12000m, stats.DonationGoal);
    }

    [Fact]
    public async Task GetStats_SumsDonations()
    {
        await SeedAsync(db =>
        {
            db.Donations.Add(Donation.Record("A", 100m, DonationCategory.OneTime));
            db.Donations.Add(Donation.Record("B", 50m, DonationCategory.Monthly));
            return db;
        });
        var stats = await Query(q => q.GetStatsAsync());
        Assert.Equal(150m, stats.TotalDonations);
    }
}

public class FinanceQueriesTests : ServiceTestBase
{
    private Task<T> Query<T>(Func<IFinanceQueries, Task<T>> action) => WithServiceAsync(action);

    [Fact]
    public async Task GetSummary_ReturnsZeroes_WhenEmpty()
    {
        var result = await Query(q => q.GetSummaryAsync(2024));
        Assert.Equal(0m, result.TotalIncome);
        Assert.Equal(0m, result.TotalExpenses);
        Assert.Equal(12, result.Monthly.Count);
    }

    [Fact]
    public async Task GetSummary_SumsDonationsAndExpenses_ForYear()
    {
        await SeedAsync(db =>
        {
            db.Donations.Add(Donation.Record("A", 300m, DonationCategory.OneTime, date: new DateTime(2024, 3, 1)));
            db.Donations.Add(Donation.Record("B", 700m, DonationCategory.Monthly, date: new DateTime(2024, 6, 1)));
            db.Expenses.Add(Expense.Record("Food", 200m, ExpenseCategory.Supplies, [(0m, 200m, 0m)], date: new DateTime(2024, 3, 15)));
            return db;
        });
        var result = await Query(q => q.GetSummaryAsync(2024));
        Assert.Equal(1000m, result.TotalIncome);
        Assert.Equal(200m, result.TotalExpenses);
    }

    [Fact]
    public async Task GetSummary_IgnoresOtherYears()
    {
        await SeedAsync(db =>
        {
            db.Donations.Add(Donation.Record("A", 100m, DonationCategory.OneTime, date: new DateTime(2023, 1, 1)));
            return db;
        });
        var result = await Query(q => q.GetSummaryAsync(2024));
        Assert.Equal(0m, result.TotalIncome);
    }

    [Fact]
    public async Task GetSummary_BreaksDownByMonth()
    {
        await SeedAsync(db =>
        {
            db.Donations.Add(Donation.Record("X", 100m, DonationCategory.OneTime, date: new DateTime(2025, 5, 1)));
            db.Expenses.Add(Expense.Record("Y", 50m, ExpenseCategory.Other, [(0m, 50m, 0m)], date: new DateTime(2025, 5, 10)));
            return db;
        });
        var result = await Query(q => q.GetSummaryAsync(2025));
        var may = result.Monthly.First(m => m.Month == 5);
        Assert.Equal(100m, may.Income);
        Assert.Equal(50m, may.Expenses);
    }
}

public class AdoptionQueriesTests : ServiceTestBase
{
    private Task<T> Query<T>(Func<IAdoptionQueries, Task<T>> action) => WithServiceAsync(action);

    private Task<Dog> SeedDog(string name = "StatDog", string breed = "Lab", DateTime? arrivalDate = null)
        => SeedAsync(db =>
        {
            var dog = Dog.CheckIn(name, breed, 12, "M", 10m, arrivalDate: arrivalDate);
            db.Dogs.Add(dog);
            return dog;
        });

    [Fact]
    public async Task GetConversionStats_ReturnsAllTwelveMonths()
    {
        var result = await Query(q => q.GetConversionStatsAsync(2025));
        Assert.Equal(12, result.Monthly.Count);
        Assert.Equal(0, result.TotalApplied);
        Assert.Equal(0, result.TotalFinalized);
    }

    [Fact]
    public async Task GetConversionStats_CountsAppliedAndFinalized()
    {
        var dog = await SeedDog();
        var year = DateTime.UtcNow.Year;
        await SeedAsync(db =>
        {
            db.Adoptions.Add(Adoption.Submit(dog.Id, "A1", null, null, AdoptionType.Adoption, null,
                createdAt: new DateTime(year, 3, 1)));
            var finalized = Adoption.Submit(dog.Id, "A2", null, null, AdoptionType.Adoption, null,
                createdAt: new DateTime(year, 3, 1));
            db.Adoptions.Add(finalized);
            return db;
        });
        // Finalize the second one through the domain (sets UpdatedAt)
        var all = await SeedAsync(db => db.Adoptions.Where(a => a.ApplicantName == "A2").ToList());
        await SeedAsync(db =>
        {
            var a = db.Adoptions.First(x => x.ApplicantName == "A2");
            a.ChangeStatus(AdoptionStatus.Finalized);
            return a;
        });

        var result = await Query(q => q.GetConversionStatsAsync(year));
        Assert.Equal(2, result.TotalApplied);
        Assert.Equal(1, result.TotalFinalized);
        var march = result.Monthly.First(m => m.Month == 3);
        Assert.Equal(2, march.Applied);
    }

    [Fact]
    public async Task GetShelterStayStats_ReturnsEmpty_WhenNoFinalizedAdoptions()
    {
        var dog = await SeedDog("StayDog");
        await SeedAsync(db =>
        {
            db.Adoptions.Add(Adoption.Submit(dog.Id, "App", null, null, AdoptionType.Adoption, null));
            return db;
        });
        var result = await Query(q => q.GetShelterStayStatsAsync());
        Assert.Empty(result.ByBreed);
    }

    [Fact]
    public async Task GetShelterStayStats_ComputesAvgStayByBreed()
    {
        var arrival = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var dog = await SeedDog("LabStay", "Labrador", arrivalDate: arrival);
        await SeedAsync(db =>
        {
            var adoption = Adoption.Submit(dog.Id, "Adopter", null, null, AdoptionType.Adoption, null);
            adoption.ChangeStatus(AdoptionStatus.Finalized);
            db.Adoptions.Add(adoption);
            return adoption;
        });

        var result = await Query(q => q.GetShelterStayStatsAsync());
        Assert.Single(result.ByBreed);
        Assert.Equal("Labrador", result.ByBreed[0].Breed);
        Assert.Equal(1, result.ByBreed[0].Count);
        Assert.True(result.ByBreed[0].AvgDays > 0);
    }
}

public class VolunteerQueriesTests : ServiceTestBase
{
    private Task<T> Query<T>(Func<IVolunteerQueries, Task<T>> action) => WithServiceAsync(action);

    [Fact]
    public async Task GetCounts_CountsByStatus()
    {
        await SeedAsync(db =>
        {
            db.Volunteers.Add(Volunteer.Register("A", "a@t.com", null, "Walker", null));
            db.Volunteers.Add(Volunteer.Register("B", "b@t.com", null, "Walker", null, status: VolunteerStatus.Inactive));
            db.Volunteers.Add(Volunteer.Register("C", "c@t.com", null, "Walker", null, status: VolunteerStatus.Pending));
            return db;
        });
        var counts = await Query(q => q.GetCountsAsync());
        Assert.Equal(3, counts.Total);
        Assert.Equal(1, counts.Active);
        Assert.Equal(1, counts.Pending);
    }

    [Fact]
    public async Task GetManagerEmails_ReturnsOnlyLoginEnabledManagers()
    {
        await SeedAsync(db =>
        {
            db.Volunteers.Add(Volunteer.Register("M1", "m1@t.com", null, "Manager", null, canLogin: true, password: "x12345"));
            db.Volunteers.Add(Volunteer.Register("M2", "m2@t.com", null, "Manager", null)); // no login
            db.Volunteers.Add(Volunteer.Register("V1", "v1@t.com", null, "Volunteer", null, canLogin: true, password: "x12345"));
            return db;
        });
        var emails = await Query(q => q.GetManagerEmailsAsync());
        Assert.Single(emails);
        Assert.Equal("m1@t.com", emails[0]);
    }
}

public class MedicalQueriesTests : ServiceTestBase
{
    private Task<T> Query<T>(Func<IMedicalQueries, Task<T>> action) => WithServiceAsync(action);

    [Fact]
    public async Task GetUpcomingVisits_ReturnsVisitsWithinHorizon()
    {
        var dog = await SeedAsync(db => { var d = Dog.CheckIn("VetDog", "Lab", 12, "M", 10m); db.Dogs.Add(d); return d; });
        await SeedAsync(db =>
        {
            db.MedicalRecords.Add(MedicalRecord.Create(dog.Id, "Dr.Soon", "Checkup", "None", null, DateTime.UtcNow.Date.AddDays(2)));
            db.MedicalRecords.Add(MedicalRecord.Create(dog.Id, "Dr.Far", "Checkup", "None", null, DateTime.UtcNow.Date.AddDays(10)));
            db.MedicalRecords.Add(MedicalRecord.Create(dog.Id, "Dr.Past", "Checkup", "None", null, DateTime.UtcNow.Date.AddDays(-1)));
            return db;
        });
        var visits = await Query(q => q.GetUpcomingVisitsAsync(3));
        Assert.Single(visits);
        Assert.Equal("Dr.Soon", visits[0].VetName);
        Assert.Equal("VetDog", visits[0].DogName);
    }
}
