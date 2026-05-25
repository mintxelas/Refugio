using Akka.Actor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Application.Messages;
using Refugio.Domain.Entities;
using Refugio.Infrastructure.Data;

namespace Refugio.Application.Actors;

public class DogActor : ReceiveActor
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DogActor(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

        ReceiveAsync<GetAllDogs>(Handle);
        ReceiveAsync<GetDogById>(Handle);
        ReceiveAsync<CreateDog>(Handle);
        ReceiveAsync<UpdateDog>(Handle);
        ReceiveAsync<DeleteDog>(Handle);
        ReceiveAsync<GetMedicalRecords>(Handle);
        ReceiveAsync<CreateMedicalRecord>(Handle);
        ReceiveAsync<GetMedications>(Handle);
        ReceiveAsync<CreateMedication>(Handle);
        ReceiveAsync<DeactivateMedication>(Handle);
        ReceiveAsync<GetDashboardStats>(Handle);
    }

    private ShelterDbContext Db(IServiceScope scope) => scope.ServiceProvider.GetRequiredService<ShelterDbContext>();

    private async Task Handle(GetAllDogs msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var q = db.Dogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(msg.Search))
            q = q.Where(d => d.Name.Contains(msg.Search) || d.Breed.Contains(msg.Search));
        if (msg.Status.HasValue)
            q = q.Where(d => d.Status == msg.Status);
        Sender.Tell(await q.OrderBy(d => d.Name).ToListAsync());
    }

    private async Task Handle(GetDogById msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var dog = await Db(scope).Dogs
            .Include(d => d.MedicalRecords)
            .Include(d => d.Medications)
            .FirstOrDefaultAsync(d => d.Id == msg.Id);
        Sender.Tell(dog);
    }

    private async Task Handle(CreateDog msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var dog = new Dog
        {
            Name = msg.Name, Breed = msg.Breed, AgeMonths = msg.AgeMonths,
            Gender = msg.Gender, WeightKg = msg.WeightKg, PhotoUrl = msg.PhotoUrl,
            Traits = msg.Traits, Notes = msg.Notes
        };
        db.Dogs.Add(dog);
        await db.SaveChangesAsync();
        Sender.Tell(dog);
    }

    private async Task Handle(UpdateDog msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var dog = await db.Dogs.FindAsync(msg.Id);
        if (dog is null) { Sender.Tell((Dog?)null); return; }
        dog.Name = msg.Name; dog.Breed = msg.Breed; dog.AgeMonths = msg.AgeMonths;
        dog.Gender = msg.Gender; dog.Status = msg.Status; dog.WeightKg = msg.WeightKg;
        dog.PhotoUrl = msg.PhotoUrl; dog.Traits = msg.Traits; dog.Notes = msg.Notes;
        await db.SaveChangesAsync();
        Sender.Tell(dog);
    }

    private async Task Handle(DeleteDog msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var dog = await db.Dogs.FindAsync(msg.Id);
        if (dog is null) { Sender.Tell(false); return; }
        db.Dogs.Remove(dog);
        await db.SaveChangesAsync();
        Sender.Tell(true);
    }

    private async Task Handle(GetMedicalRecords msg)
    {
        using var scope = _scopeFactory.CreateScope();
        Sender.Tell(await Db(scope).MedicalRecords
            .Where(r => r.DogId == msg.DogId)
            .OrderByDescending(r => r.VisitDate)
            .ToListAsync());
    }

    private async Task Handle(CreateMedicalRecord msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var record = new MedicalRecord
        {
            DogId = msg.DogId, VetName = msg.VetName, Diagnosis = msg.Diagnosis,
            Treatment = msg.Treatment, Notes = msg.Notes, NextVisitDate = msg.NextVisitDate,
            VisitDate = DateTime.UtcNow
        };
        db.MedicalRecords.Add(record);
        await db.SaveChangesAsync();
        Sender.Tell(record);
    }

    private async Task Handle(GetMedications msg)
    {
        using var scope = _scopeFactory.CreateScope();
        Sender.Tell(await Db(scope).Medications
            .Where(m => m.DogId == msg.DogId)
            .OrderByDescending(m => m.StartDate)
            .ToListAsync());
    }

    private async Task Handle(CreateMedication msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var med = new Medication
        {
            DogId = msg.DogId, Name = msg.Name, Dosage = msg.Dosage,
            Frequency = msg.Frequency, StartDate = msg.StartDate, EndDate = msg.EndDate
        };
        db.Medications.Add(med);
        await db.SaveChangesAsync();
        Sender.Tell(med);
    }

    private async Task Handle(DeactivateMedication msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var med = await db.Medications.FindAsync(msg.MedicationId);
        if (med is null) { Sender.Tell(false); return; }
        med.IsActive = false;
        await db.SaveChangesAsync();
        Sender.Tell(true);
    }

    private async Task Handle(GetDashboardStats msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var totalDogs = await db.Dogs.CountAsync();
        var adoptionsThisWeek = await db.Adoptions
            .Where(a => a.Status == AdoptionStatus.Finalized && a.UpdatedAt >= DateTime.UtcNow.AddDays(-7))
            .CountAsync();
        var urgentMeds = await db.Medications
            .Where(m => m.IsActive && m.EndDate <= DateTime.UtcNow.AddDays(3))
            .CountAsync();
        var donationGoal = 12000m;
        var totalDonations = await db.Donations.SumAsync(d => (decimal?)d.Amount) ?? 0;
        Sender.Tell(new DashboardStats(totalDogs, adoptionsThisWeek, urgentMeds, totalDonations, donationGoal));
    }
}

public record DashboardStats(int TotalDogs, int NewAdoptions, int UrgentMeds, decimal TotalDonations, decimal DonationGoal);
