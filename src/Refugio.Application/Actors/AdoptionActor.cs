using Akka.Actor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Application.Messages;
using Refugio.Application.Services;
using Refugio.Domain.Entities;

namespace Refugio.Application.Actors;

public class AdoptionActor : ShelterActorBase
{
    public AdoptionActor(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
        ReceiveAsync<GetAllAdoptions>(Handle);
        ReceiveAsync<GetAdoptionsPaged>(Handle);
        ReceiveAsync<GetAdoptionById>(Handle);
        ReceiveAsync<CreateAdoption>(Handle);
        ReceiveAsync<UpdateAdoption>(Handle);
        ReceiveAsync<UpdateAdoptionStatus>(Handle);
        ReceiveAsync<DeleteAdoption>(msg => SoftDelete<Adoption>(msg.Id));
        ReceiveAsync<GetDeletedAdoptions>(_ => GetDeleted<Adoption>(q => q.Include(a => a.Dog)));
        ReceiveAsync<RestoreAdoption>(msg => Restore<Adoption>(msg.Id));
        ReceiveAsync<GetAdoptionConversionStats>(Handle);
        ReceiveAsync<GetShelterStayStats>(Handle);
    }

    private Task Handle(GetAllAdoptions msg) => WithDb(async db =>
    {
        var q = db.Adoptions.AsNoTracking().Include(a => a.Dog).AsQueryable();
        if (msg.Status.HasValue) q = q.Where(a => a.Status == msg.Status);
        Sender.Tell(await q.OrderByDescending(a => a.CreatedAt).ToListAsync());
    });

    private Task Handle(GetAdoptionsPaged msg) => WithDb(async db =>
    {
        var q = db.Adoptions.AsNoTracking().Include(a => a.Dog).AsQueryable();
        if (msg.Status.HasValue) q = q.Where(a => a.Status == msg.Status);
        Sender.Tell(await q.OrderByDescending(a => a.CreatedAt).ToPageAsync(msg.Page, msg.PageSize));
    });

    private Task Handle(GetAdoptionById msg) => WithDb(async db =>
        Sender.Tell(await db.Adoptions.AsNoTracking().Include(a => a.Dog).FirstOrDefaultAsync(a => a.Id == msg.Id)));

    private Task Handle(CreateAdoption msg) => WithDb(async db =>
    {
        var adoption = new Adoption
        {
            DogId = msg.DogId, ApplicantName = msg.ApplicantName,
            ApplicantEmail = msg.ApplicantEmail, ApplicantPhone = msg.ApplicantPhone,
            Type = msg.Type, Notes = msg.Notes
        };
        db.Adoptions.Add(adoption);
        await db.SaveChangesAsync();
        Sender.Tell(adoption);
    });

    private Task Handle(UpdateAdoption msg) => WithDb(async db =>
    {
        var adoption = await db.Adoptions.FindAsync(msg.Id);
        if (adoption is null) { Sender.Tell((Adoption?)null); return; }
        adoption.ApplicantName = msg.ApplicantName;
        adoption.ApplicantEmail = msg.ApplicantEmail;
        adoption.ApplicantPhone = msg.ApplicantPhone;
        adoption.Type = msg.Type;
        adoption.Status = msg.Status;
        adoption.Notes = msg.Notes;
        adoption.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        Sender.Tell(adoption);
    });

    private Task Handle(UpdateAdoptionStatus msg) => WithDb(async (db, services) =>
    {
        var adoption = await db.Adoptions.FindAsync(msg.Id);
        if (adoption is null) { Sender.Tell((Adoption?)null); return; }
        adoption.Status = msg.NewStatus;
        adoption.UpdatedAt = DateTime.UtcNow;
        if (msg.Notes is not null) adoption.Notes = msg.Notes;
        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(adoption.ApplicantEmail))
        {
            var emailSender = services.GetService<IShelterEmailSender>();
            if (emailSender is not null)
                await emailSender.SendAsync(
                    adoption.ApplicantEmail,
                    $"Application update for {adoption.ApplicantName}",
                    $"Your adoption application status has been updated to: {msg.NewStatus}.");
        }

        Sender.Tell(adoption);
    });

    private Task Handle(GetAdoptionConversionStats msg) => WithDb(async db =>
    {
        var applied = await db.Adoptions
            .Where(a => a.CreatedAt.Year == msg.Year)
            .GroupBy(a => a.CreatedAt.Month)
            .Select(g => new { Month = g.Key, Count = g.Count() })
            .ToListAsync();

        var finalized = await db.Adoptions
            .Where(a => a.Status == AdoptionStatus.Finalized && a.UpdatedAt != null && a.UpdatedAt.Value.Year == msg.Year)
            .GroupBy(a => a.UpdatedAt!.Value.Month)
            .Select(g => new { Month = g.Key, Count = g.Count() })
            .ToListAsync();

        var monthly = Enumerable.Range(1, 12)
            .Select(m => new MonthlyConversionData(
                m,
                applied.FirstOrDefault(x => x.Month == m)?.Count ?? 0,
                finalized.FirstOrDefault(x => x.Month == m)?.Count ?? 0))
            .ToList();

        Sender.Tell(new AdoptionConversionStats(monthly, applied.Sum(x => x.Count), finalized.Sum(x => x.Count)));
    });

    private Task Handle(GetShelterStayStats msg) => WithDb(async db =>
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

        Sender.Tell(new ShelterStayStats(byBreed));
    });
}
