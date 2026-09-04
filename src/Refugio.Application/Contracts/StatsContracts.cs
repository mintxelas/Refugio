namespace Refugio.Application.Contracts;

public record DashboardStats(int TotalDogs, int NewAdoptions, int UrgentMeds, decimal TotalDonations, decimal DonationGoal);

public record MonthSummary(int Month, decimal Income, decimal Expenses);
public record FinanceSummary(decimal TotalIncome, decimal TotalExpenses, List<MonthSummary> Monthly);

public record MonthlyConversionData(int Month, int Applied, int Finalized);
public record AdoptionConversionStats(List<MonthlyConversionData> Monthly, int TotalApplied, int TotalFinalized);

public record BreedStayData(string Breed, double AvgDays, int Count);
public record ShelterStayStats(List<BreedStayData> ByBreed);

public record VolunteerCounts(int Total, int Active, int Pending);

public record UpcomingVisit(string DogName, DateTime NextVisitDate, string VetName, string Diagnosis);

public record UrgentMedicationDto(int DogId, string DogName, int MedicationId, string Name, string Dosage, string Frequency, DateTime? EndDate);
