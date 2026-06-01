using Refugio.Domain.Entities;
using Refugio.Domain.Helpers;

namespace Refugio.Infrastructure.Data;

public static class SeedData
{
    public static void Seed(ShelterDbContext db)
    {
        if (db.Dogs.Any()) return;

        var dogs = new List<Dog>
        {
            new() { Name = "Cooper", Breed = "Golden Retriever", AgeMonths = 24, Gender = "Male", Status = DogStatus.Medical, Traits = "Neutered,High Energy", WeightKg = 28.5m, ArrivalDate = DateTime.UtcNow.AddMonths(-3) },
            new() { Name = "Luna", Breed = "Beagle Mix", AgeMonths = 48, Gender = "Female", Status = DogStatus.Available, Traits = "Kid Friendly,Quiet", WeightKg = 12.3m, ArrivalDate = DateTime.UtcNow.AddMonths(-6) },
            new() { Name = "Max", Breed = "Terrier", AgeMonths = 6, Gender = "Male", Status = DogStatus.Available, Traits = "Vaccinations Pending", WeightKg = 4.2m, ArrivalDate = DateTime.UtcNow.AddDays(-7) },
            new() { Name = "Bella", Breed = "Labrador", AgeMonths = 36, Gender = "Female", Status = DogStatus.Available, Traits = "Friendly,Trained", WeightKg = 25.0m, ArrivalDate = DateTime.UtcNow.AddMonths(-2) },
            new() { Name = "Rex", Breed = "German Shepherd", AgeMonths = 18, Gender = "Male", Status = DogStatus.Foster, Traits = "Alert,Loyal", WeightKg = 32.0m, ArrivalDate = DateTime.UtcNow.AddMonths(-1) },
            new() { Name = "Toby", Breed = "Mixed Breed", AgeMonths = 30, Gender = "Male", Status = DogStatus.Available, Traits = "Gentle,Calm", WeightKg = 15.0m, ArrivalDate = DateTime.UtcNow.AddMonths(-4) },
            new() { Name = "Daisy", Breed = "Poodle", AgeMonths = 12, Gender = "Female", Status = DogStatus.Available, Traits = "Smart,Playful", WeightKg = 8.5m, ArrivalDate = DateTime.UtcNow.AddDays(-14) },
            new() { Name = "Rocky", Breed = "Bulldog", AgeMonths = 60, Gender = "Male", Status = DogStatus.Medical, Traits = "Laid Back,Snorer", WeightKg = 22.0m, ArrivalDate = DateTime.UtcNow.AddMonths(-5) },
        };
        db.Dogs.AddRange(dogs);
        db.SaveChanges();

        db.MedicalRecords.AddRange(
            new MedicalRecord { DogId = dogs[0].Id, VetName = "Dr. Torres", Diagnosis = "Ear infection", Treatment = "Antibiotic drops", VisitDate = DateTime.UtcNow.AddDays(-5), NextVisitDate = DateTime.UtcNow.AddDays(9) },
            new MedicalRecord { DogId = dogs[3].Id, VetName = "Dr. Torres", Diagnosis = "Routine X-Ray", Treatment = "Observation", VisitDate = DateTime.UtcNow.AddDays(-2) },
            new MedicalRecord { DogId = dogs[7].Id, VetName = "Dr. Reyes", Diagnosis = "Hip dysplasia monitoring", Treatment = "Anti-inflammatory meds", VisitDate = DateTime.UtcNow.AddDays(-10) }
        );

        db.Medications.AddRange(
            new Medication { DogId = dogs[0].Id, Name = "Otomax drops", Dosage = "3 drops", Frequency = "Twice daily", StartDate = DateTime.UtcNow.AddDays(-5), EndDate = DateTime.UtcNow.AddDays(9) },
            new Medication { DogId = dogs[7].Id, Name = "Meloxicam", Dosage = "5mg", Frequency = "Once daily", StartDate = DateTime.UtcNow.AddDays(-10) }
        );

        db.Adoptions.AddRange(
            new Adoption { DogId = dogs[4].Id, ApplicantName = "Familia García", ApplicantEmail = "garcia@email.com", Type = AdoptionType.Adoption, Status = AdoptionStatus.Applied, CreatedAt = DateTime.UtcNow.AddHours(-2) },
            new Adoption { DogId = dogs[1].Id, ApplicantName = "Marta Jiménez", ApplicantEmail = "marta@email.com", Type = AdoptionType.Foster, Status = AdoptionStatus.Applied, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new Adoption { DogId = dogs[5].Id, ApplicantName = "Robert & Sarah", ApplicantEmail = "roberts@email.com", Type = AdoptionType.Adoption, Status = AdoptionStatus.Interview, CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new Adoption { DogId = dogs[3].Id, ApplicantName = "Thompson Family", ApplicantEmail = "thompson@email.com", Type = AdoptionType.Adoption, Status = AdoptionStatus.HomeCheck, CreatedAt = DateTime.UtcNow.AddDays(-7) }
        );

        db.Tasks.AddRange(
            new ShelterTask { Title = "Morning Walk - Group A", DueDateTime = DateTime.Today.AddHours(8.5), Location = "Main Park" },
            new ShelterTask { Title = "Vet Visit: Bella (X-Ray)", DueDateTime = DateTime.Today.AddHours(10.25), Location = "City Pet Hospital" },
            new ShelterTask { Title = "Donation Sorting", DueDateTime = DateTime.Today.AddHours(13), Location = "Main Hall" },
            new ShelterTask { Title = "Adoption Interview: Thompson", DueDateTime = DateTime.Today.AddHours(15.5), Location = "Meeting Room 2" }
        );

        db.Donations.AddRange(
            new Donation { DonorName = "James Robertson", Amount = 120m, Date = DateTime.UtcNow.AddDays(-1), Category = DonationCategory.Monthly },
            new Donation { DonorName = "Sarah Miller", Amount = 50m, Date = DateTime.UtcNow.AddDays(-2), Category = DonationCategory.OneTime },
            new Donation { DonorName = "PetCare Corp", Amount = 500m, Date = DateTime.UtcNow.AddDays(-5), Category = DonationCategory.Corporate },
            new Donation { DonorName = "Anonymous", Amount = 25m, Date = DateTime.UtcNow.AddDays(-3), Category = DonationCategory.OneTime },
            new Donation { DonorName = "Maria Lopez", Amount = 75m, Date = DateTime.UtcNow.AddDays(-4), Category = DonationCategory.Monthly }
        );

        db.Expenses.AddRange(
            new Expense { Description = "Veterinary supplies", Amount = 340m, Date = DateTime.UtcNow.AddDays(-3), Category = ExpenseCategory.Medical },
            new Expense { Description = "Dog food (bulk)", Amount = 210m, Date = DateTime.UtcNow.AddDays(-5), Category = ExpenseCategory.Food },
            new Expense { Description = "Kennel maintenance", Amount = 150m, Date = DateTime.UtcNow.AddDays(-7), Category = ExpenseCategory.Facilities }
        );

        db.Volunteers.AddRange(
            new Volunteer { Name = "Elena Smith", Email = "elena@havensanctuary.org", Role = "Manager", Status = VolunteerStatus.Active, JoinDate = DateTime.UtcNow.AddYears(-2), CanLogin = true, PasswordHash = PasswordHelper.Hash("shelter123") },
            new Volunteer { Name = "Carlos Ruiz", Email = "carlos@email.com", Phone = "555-0101", Role = "Volunteer", Status = VolunteerStatus.Active, JoinDate = DateTime.UtcNow.AddMonths(-8) },
            new Volunteer { Name = "Ana Pérez", Email = "ana@email.com", Phone = "555-0102", Role = "Volunteer", Status = VolunteerStatus.Active, JoinDate = DateTime.UtcNow.AddMonths(-14) },
            new Volunteer { Name = "Luis García", Email = "luis@email.com", Phone = "555-0103", Role = "Volunteer", Status = VolunteerStatus.Inactive, JoinDate = DateTime.UtcNow.AddMonths(-20) }
        );

        db.Events.AddRange(
            new ShelterEvent { Title = "Morning Walk Group A", StartDateTime = DateTime.Today.AddHours(8.5), EndDateTime = DateTime.Today.AddHours(9.5), Location = "Main Park", EventType = "Walk", AssignedVolunteers = 4 },
            new ShelterEvent { Title = "Adoption Day", StartDateTime = DateTime.Today.AddDays(3).AddHours(10), EndDateTime = DateTime.Today.AddDays(3).AddHours(17), Location = "Main Hall", EventType = "Adoption", AssignedVolunteers = 6 },
            new ShelterEvent { Title = "Vet Checkups", StartDateTime = DateTime.Today.AddDays(1).AddHours(9), EndDateTime = DateTime.Today.AddDays(1).AddHours(12), Location = "Clinic", EventType = "Medical", AssignedVolunteers = 2 }
        );

        db.Goals.AddRange(
            new Goal { Title = "Rescue Van 2024", Description = "Replacing our oldest ambulance with a specialized pet transport unit.", TargetAmount = 45000m, CurrentAmount = 18750m, Deadline = new DateTime(DateTime.UtcNow.Year, 12, 31) },
            new Goal { Title = "Winter Shelter Heating", Description = "New heating system for the kennels before the cold season.", TargetAmount = 8000m, CurrentAmount = 3200m, Deadline = DateTime.UtcNow.AddMonths(2) },
            new Goal { Title = "Emergency Medical Fund", Description = "Reserve for unexpected surgeries and critical care.", TargetAmount = 15000m, CurrentAmount = 15000m }
        );

        db.Settings.Add(new ShelterSettings { Name = "Haven Sanctuary", Phrase = "City Main Branch" });

        db.SaveChanges();
    }
}
