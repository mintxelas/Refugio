using Akka.Actor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Application.Messages;
using Refugio.Domain.Entities;
using Refugio.Infrastructure.Data;

namespace Refugio.Application.Actors;

public class DogActor : ShelterActorBase
{
    public DogActor(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
        ReceiveAsync<GetAllDogs>(Handle);
        ReceiveAsync<GetDogsPaged>(Handle);
        ReceiveAsync<GetDogById>(Handle);
        ReceiveAsync<CreateDog>(Handle);
        ReceiveAsync<UpdateDog>(Handle);
        ReceiveAsync<DeleteDog>(msg => SoftDelete<Dog>(msg.Id));
        ReceiveAsync<UpdateDogPhoto>(Handle);
        ReceiveAsync<GetMedicalRecords>(Handle);
        ReceiveAsync<GetMedicalRecordById>(Handle);
        ReceiveAsync<CreateMedicalRecord>(Handle);
        ReceiveAsync<UpdateMedicalRecord>(Handle);
        ReceiveAsync<DeleteMedicalRecord>(msg => SoftDelete<MedicalRecord>(msg.Id));
        ReceiveAsync<GetMedications>(Handle);
        ReceiveAsync<GetMedicationById>(Handle);
        ReceiveAsync<CreateMedication>(Handle);
        ReceiveAsync<UpdateMedication>(Handle);
        ReceiveAsync<DeleteMedication>(msg => SoftDelete<Medication>(msg.Id));
        ReceiveAsync<DeactivateMedication>(Handle);
        ReceiveAsync<GetDashboardStats>(Handle);
        ReceiveAsync<GetDeletedDogs>(_ => GetDeleted<Dog>());
        ReceiveAsync<RestoreDog>(msg => Restore<Dog>(msg.Id));
        ReceiveAsync<GetDeletedDogById>(msg => GetDeletedById<Dog>(msg.Id, q => q.Include(d => d.MedicalRecords).Include(d => d.Medications)));
        ReceiveAsync<PermanentDeleteDog>(msg => PermanentDelete<Dog>(msg.Id));
        ReceiveAsync<GetDeletedMedicalRecords>(_ => GetDeleted<MedicalRecord>(q => q.Include(r => r.Dog)));
        ReceiveAsync<RestoreMedicalRecord>(msg => Restore<MedicalRecord>(msg.Id, DogIsAlive));
        ReceiveAsync<GetDeletedMedicalRecordById>(msg => GetDeletedById<MedicalRecord>(msg.Id, q => q.Include(r => r.Dog)));
        ReceiveAsync<PermanentDeleteMedicalRecord>(msg => PermanentDelete<MedicalRecord>(msg.Id));
        ReceiveAsync<GetDeletedMedications>(_ => GetDeleted<Medication>(q => q.Include(m => m.Dog)));
        ReceiveAsync<RestoreMedication>(msg => Restore<Medication>(msg.Id, DogIsAlive));
        ReceiveAsync<GetDeletedMedicationById>(msg => GetDeletedById<Medication>(msg.Id, q => q.Include(m => m.Dog)));
        ReceiveAsync<PermanentDeleteMedication>(msg => PermanentDelete<Medication>(msg.Id));
    }

    // A child record may only be restored while its parent dog is still alive.
    // Uses the normal (filtered) query, so a soft-deleted dog returns false.
    private static Task<bool> DogIsAlive(ShelterDbContext db, MedicalRecord rec)
        => db.Dogs.AnyAsync(d => d.Id == rec.DogId);
    private static Task<bool> DogIsAlive(ShelterDbContext db, Medication med)
        => db.Dogs.AnyAsync(d => d.Id == med.DogId);

    private Task Handle(GetAllDogs msg) => WithDb(async db =>
    {
        var q = db.Dogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(msg.Search))
            q = q.Where(d => d.Name.Contains(msg.Search) || d.Breed.Contains(msg.Search));
        if (msg.Status.HasValue)
            q = q.Where(d => d.Status == msg.Status);
        Sender.Tell(await q.OrderBy(d => d.Name).ToListAsync());
    });

    private Task Handle(GetDogsPaged msg) => WithDb(async db =>
    {
        var q = db.Dogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(msg.Search))
            q = q.Where(d => d.Name.Contains(msg.Search) || d.Breed.Contains(msg.Search));
        if (msg.Status.HasValue)
            q = q.Where(d => d.Status == msg.Status);
        Sender.Tell(await q.OrderBy(d => d.Name).ToPageAsync(msg.Page, msg.PageSize));
    });

    private Task Handle(GetDogById msg) => WithDb(async db =>
        Sender.Tell(await db.Dogs
            .Include(d => d.MedicalRecords)
            .Include(d => d.Medications)
            .FirstOrDefaultAsync(d => d.Id == msg.Id)));

    private Task Handle(CreateDog msg) => WithDb(async db =>
    {
        var dog = new Dog
        {
            Name = msg.Name, Breed = msg.Breed, AgeMonths = msg.AgeMonths,
            Gender = msg.Gender, WeightKg = msg.WeightKg, PhotoUrl = msg.PhotoUrl,
            Traits = msg.Traits, Notes = msg.Notes
        };
        db.Dogs.Add(dog);
        await db.SaveChangesAsync();
        Sender.Tell(dog);
    });

    private Task Handle(UpdateDog msg) => WithDb(async db =>
    {
        var dog = await db.Dogs.FindAsync(msg.Id);
        if (dog is null) { Sender.Tell((Dog?)null); return; }
        dog.Name = msg.Name; dog.Breed = msg.Breed; dog.AgeMonths = msg.AgeMonths;
        dog.Gender = msg.Gender; dog.Status = msg.Status; dog.WeightKg = msg.WeightKg;
        dog.PhotoUrl = msg.PhotoUrl; dog.Traits = msg.Traits; dog.Notes = msg.Notes;
        await db.SaveChangesAsync();
        Sender.Tell(dog);
    });

    private Task Handle(UpdateDogPhoto msg) => WithDb(async db =>
    {
        var dog = await db.Dogs.FindAsync(msg.Id);
        if (dog is null) { Sender.Tell(false); return; }
        dog.PhotoUrl = msg.PhotoUrl;
        await db.SaveChangesAsync();
        Sender.Tell(true);
    });

    private Task Handle(GetMedicalRecords msg) => WithDb(async db =>
        Sender.Tell(await db.MedicalRecords
            .Where(r => r.DogId == msg.DogId)
            .OrderByDescending(r => r.VisitDate)
            .ToListAsync()));

    private Task Handle(CreateMedicalRecord msg) => WithDb(async db =>
    {
        var record = new MedicalRecord
        {
            DogId = msg.DogId, VetName = msg.VetName, Diagnosis = msg.Diagnosis,
            Treatment = msg.Treatment, Notes = msg.Notes, NextVisitDate = msg.NextVisitDate,
            VisitDate = DateTime.UtcNow
        };
        db.MedicalRecords.Add(record);
        await db.SaveChangesAsync();
        Sender.Tell(record);
    });

    private Task Handle(GetMedicalRecordById msg) => WithDb(async db =>
        Sender.Tell(await db.MedicalRecords.FirstOrDefaultAsync(r => r.Id == msg.Id)));

    private Task Handle(UpdateMedicalRecord msg) => WithDb(async db =>
    {
        var rec = await db.MedicalRecords.FindAsync(msg.Id);
        if (rec is null) { Sender.Tell((MedicalRecord?)null); return; }
        rec.VetName = msg.VetName; rec.Diagnosis = msg.Diagnosis;
        rec.Treatment = msg.Treatment; rec.Notes = msg.Notes;
        rec.VisitDate = msg.VisitDate; rec.NextVisitDate = msg.NextVisitDate;
        await db.SaveChangesAsync();
        Sender.Tell(rec);
    });

    private Task Handle(GetMedications msg) => WithDb(async db =>
        Sender.Tell(await db.Medications
            .Where(m => m.DogId == msg.DogId)
            .OrderByDescending(m => m.StartDate)
            .ToListAsync()));

    private Task Handle(CreateMedication msg) => WithDb(async db =>
    {
        var med = new Medication
        {
            DogId = msg.DogId, Name = msg.Name, Dosage = msg.Dosage,
            Frequency = msg.Frequency, StartDate = msg.StartDate, EndDate = msg.EndDate
        };
        db.Medications.Add(med);
        await db.SaveChangesAsync();
        Sender.Tell(med);
    });

    private Task Handle(GetMedicationById msg) => WithDb(async db =>
        Sender.Tell(await db.Medications.FirstOrDefaultAsync(m => m.Id == msg.Id)));

    private Task Handle(UpdateMedication msg) => WithDb(async db =>
    {
        var med = await db.Medications.FindAsync(msg.Id);
        if (med is null) { Sender.Tell((Medication?)null); return; }
        med.Name = msg.Name; med.Dosage = msg.Dosage; med.Frequency = msg.Frequency;
        med.StartDate = msg.StartDate; med.EndDate = msg.EndDate; med.IsActive = msg.IsActive;
        await db.SaveChangesAsync();
        Sender.Tell(med);
    });

    private Task Handle(DeactivateMedication msg) => WithDb(async db =>
    {
        var med = await db.Medications.FindAsync(msg.MedicationId);
        if (med is null) { Sender.Tell(false); return; }
        med.IsActive = false;
        await db.SaveChangesAsync();
        Sender.Tell(true);
    });

    private Task Handle(GetDashboardStats msg) => WithDb(async db =>
    {
        var totalDogs = await db.Dogs.CountAsync();
        var adoptionsThisWeek = await db.Adoptions
            .Where(a => a.Status == AdoptionStatus.Finalized && a.UpdatedAt >= DateTime.UtcNow.AddDays(-7))
            .CountAsync();
        var urgentMeds = await db.Medications
            .Where(m => m.IsActive && m.EndDate <= DateTime.UtcNow.AddDays(3))
            .CountAsync();
        const decimal donationGoal = 12000m;
        var totalDonations = await db.Donations.SumAsync(d => (decimal?)d.Amount) ?? 0;
        Sender.Tell(new DashboardStats(totalDogs, adoptionsThisWeek, urgentMeds, totalDonations, donationGoal));
    });
}

public record DashboardStats(int TotalDogs, int NewAdoptions, int UrgentMeds, decimal TotalDonations, decimal DonationGoal);
