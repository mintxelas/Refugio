using Refugio.Domain.Entities;

namespace Refugio.Infrastructure.Data;

public static class SeedData
{
    public static void Seed(ShelterDbContext db)
    {
        if (db.Dogs.Any()) return;

        var dogs = new List<Dog>
        {
            Dog.CheckIn("Cooper", "Golden Retriever", 24, "Male", 28.5m, traits: "Neutered,High Energy", status: DogStatus.Medical, arrivalDate: DateTime.UtcNow.AddMonths(-3)),
            Dog.CheckIn("Luna", "Beagle Mix", 48, "Female", 12.3m, traits: "Kid Friendly,Quiet", arrivalDate: DateTime.UtcNow.AddMonths(-6)),
            Dog.CheckIn("Max", "Terrier", 6, "Male", 4.2m, traits: "Vaccinations Pending", arrivalDate: DateTime.UtcNow.AddDays(-7)),
            Dog.CheckIn("Bella", "Labrador", 36, "Female", 25.0m, traits: "Friendly,Trained", arrivalDate: DateTime.UtcNow.AddMonths(-2)),
            Dog.CheckIn("Rex", "German Shepherd", 18, "Male", 32.0m, traits: "Alert,Loyal", status: DogStatus.Foster, arrivalDate: DateTime.UtcNow.AddMonths(-1)),
            Dog.CheckIn("Toby", "Mixed Breed", 30, "Male", 15.0m, traits: "Gentle,Calm", arrivalDate: DateTime.UtcNow.AddMonths(-4)),
            Dog.CheckIn("Daisy", "Poodle", 12, "Female", 8.5m, traits: "Smart,Playful", arrivalDate: DateTime.UtcNow.AddDays(-14)),
            Dog.CheckIn("Rocky", "Bulldog", 60, "Male", 22.0m, traits: "Laid Back,Snorer", status: DogStatus.Medical, arrivalDate: DateTime.UtcNow.AddMonths(-5)),
        };
        db.Dogs.AddRange(dogs);
        db.SaveChanges();

        db.MedicalRecords.AddRange(
            MedicalRecord.Create(dogs[0].Id, "Dr. Torres", "Ear infection", "Antibiotic drops", null, DateTime.UtcNow.AddDays(9), DateTime.UtcNow.AddDays(-5)),
            MedicalRecord.Create(dogs[3].Id, "Dr. Torres", "Routine X-Ray", "Observation", null, null, DateTime.UtcNow.AddDays(-2)),
            MedicalRecord.Create(dogs[7].Id, "Dr. Reyes", "Hip dysplasia monitoring", "Anti-inflammatory meds", null, null, DateTime.UtcNow.AddDays(-10)));

        db.Medications.AddRange(
            Medication.Create(dogs[0].Id, "Otomax drops", "3 drops", "Twice daily", DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(9)),
            Medication.Create(dogs[7].Id, "Meloxicam", "5mg", "Once daily", DateTime.UtcNow.AddDays(-10), null));

        db.Adoptions.AddRange(
            Adoption.Submit(dogs[4].Id, "Familia García", "garcia@email.com", null, AdoptionType.Adoption, null, createdAt: DateTime.UtcNow.AddHours(-2)),
            Adoption.Submit(dogs[1].Id, "Marta Jiménez", "marta@email.com", null, AdoptionType.Foster, null, createdAt: DateTime.UtcNow.AddDays(-1)),
            Adoption.Submit(dogs[5].Id, "Robert & Sarah", "roberts@email.com", null, AdoptionType.Adoption, null, AdoptionStatus.Interview, DateTime.UtcNow.AddDays(-3)),
            Adoption.Submit(dogs[3].Id, "Thompson Family", "thompson@email.com", null, AdoptionType.Adoption, null, AdoptionStatus.HomeCheck, DateTime.UtcNow.AddDays(-7)));

        db.Tasks.AddRange(
            ShelterTask.Create("Morning Walk - Group A", DateTime.Today.AddHours(8.5), location: "Main Park"),
            ShelterTask.Create("Vet Visit: Bella (X-Ray)", DateTime.Today.AddHours(10.25), location: "City Pet Hospital"),
            ShelterTask.Create("Donation Sorting", DateTime.Today.AddHours(13), location: "Main Hall"),
            ShelterTask.Create("Adoption Interview: Thompson", DateTime.Today.AddHours(15.5), location: "Meeting Room 2"));

        db.Donations.AddRange(
            Donation.Record("James Robertson", 120m, DonationCategory.Monthly, date: DateTime.UtcNow.AddDays(-1)),
            Donation.Record("Sarah Miller", 50m, DonationCategory.OneTime, date: DateTime.UtcNow.AddDays(-2)),
            Donation.Record("PetCare Corp", 500m, DonationCategory.Corporate, date: DateTime.UtcNow.AddDays(-5)),
            Donation.Record("Anonymous", 25m, DonationCategory.OneTime, date: DateTime.UtcNow.AddDays(-3)),
            Donation.Record("Maria Lopez", 75m, DonationCategory.Monthly, date: DateTime.UtcNow.AddDays(-4)));

        db.Expenses.AddRange(
            Expense.Record("Veterinary supplies", 340m, ExpenseCategory.Medical, date: DateTime.UtcNow.AddDays(-3)),
            Expense.Record("Dog food (bulk)", 210m, ExpenseCategory.Food, date: DateTime.UtcNow.AddDays(-5)),
            Expense.Record("Kennel maintenance", 150m, ExpenseCategory.Facilities, date: DateTime.UtcNow.AddDays(-7)));

        db.Volunteers.AddRange(
            Volunteer.Register("Elena Smith", "elena@havensanctuary.org", null, "Manager", null, canLogin: true, password: "shelter123", joinDate: DateTime.UtcNow.AddYears(-2)),
            Volunteer.Register("Carlos Ruiz", "carlos@email.com", "555-0101", "Volunteer", null, joinDate: DateTime.UtcNow.AddMonths(-8)),
            Volunteer.Register("Ana Pérez", "ana@email.com", "555-0102", "Volunteer", null, joinDate: DateTime.UtcNow.AddMonths(-14)),
            Volunteer.Register("Luis García", "luis@email.com", "555-0103", "Volunteer", null, status: VolunteerStatus.Inactive, joinDate: DateTime.UtcNow.AddMonths(-20)));

        db.Events.AddRange(
            ShelterEvent.Schedule("Morning Walk Group A", DateTime.Today.AddHours(8.5), DateTime.Today.AddHours(9.5), "Main Park", eventType: "Walk", assignedVolunteers: 4),
            ShelterEvent.Schedule("Adoption Day", DateTime.Today.AddDays(3).AddHours(10), DateTime.Today.AddDays(3).AddHours(17), "Main Hall", eventType: "Adoption", assignedVolunteers: 6),
            ShelterEvent.Schedule("Vet Checkups", DateTime.Today.AddDays(1).AddHours(9), DateTime.Today.AddDays(1).AddHours(12), "Clinic", eventType: "Medical", assignedVolunteers: 2));

        db.Goals.AddRange(
            Goal.Create("Rescue Van 2024", "Replacing our oldest ambulance with a specialized pet transport unit.", 45000m, 18750m, new DateTime(DateTime.UtcNow.Year, 12, 31)),
            Goal.Create("Winter Shelter Heating", "New heating system for the kennels before the cold season.", 8000m, 3200m, DateTime.UtcNow.AddMonths(2)),
            Goal.Create("Emergency Medical Fund", "Reserve for unexpected surgeries and critical care.", 15000m, 15000m));

        db.Settings.Add(ShelterSettings.CreateDefault());

        db.SaveChanges();
    }
}
