using Akka.Actor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Application.Messages;
using Refugio.Application.Services;
using Refugio.Domain.Entities;
using Refugio.Infrastructure.Data;

namespace Refugio.Application.Actors;

public class AdoptionActor : ReceiveActor
{
    private readonly IServiceScopeFactory _scopeFactory;

    public AdoptionActor(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        ReceiveAsync<GetAllAdoptions>(Handle);
        ReceiveAsync<GetAdoptionsPaged>(Handle);
        ReceiveAsync<GetAdoptionById>(Handle);
        ReceiveAsync<CreateAdoption>(Handle);
        ReceiveAsync<UpdateAdoption>(Handle);
        ReceiveAsync<UpdateAdoptionStatus>(Handle);
        ReceiveAsync<DeleteAdoption>(Handle);
        ReceiveAsync<GetDeletedAdoptions>(Handle);
        ReceiveAsync<RestoreAdoption>(Handle);
        ReceiveAsync<GetAdoptionConversionStats>(Handle);
        ReceiveAsync<GetShelterStayStats>(Handle);
    }

    private ShelterDbContext Db(IServiceScope s) => s.ServiceProvider.GetRequiredService<ShelterDbContext>();

    private async Task Handle(GetAllAdoptions msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var q = Db(scope).Adoptions.AsNoTracking().Include(a => a.Dog).AsQueryable();
        if (msg.Status.HasValue) q = q.Where(a => a.Status == msg.Status);
        Sender.Tell(await q.OrderByDescending(a => a.CreatedAt).ToListAsync());
    }

    private async Task Handle(GetAdoptionsPaged msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var q = Db(scope).Adoptions.AsNoTracking().Include(a => a.Dog).AsQueryable();
        if (msg.Status.HasValue) q = q.Where(a => a.Status == msg.Status);
        q = q.OrderByDescending(a => a.CreatedAt);
        var total = await q.CountAsync();
        var items = await q.Skip((msg.Page - 1) * msg.PageSize).Take(msg.PageSize).ToListAsync();
        Sender.Tell(new AdoptionPage(items, total, msg.Page, msg.PageSize));
    }

    private async Task Handle(GetAdoptionById msg)
    {
        using var scope = _scopeFactory.CreateScope();
        Sender.Tell(await Db(scope).Adoptions.AsNoTracking().Include(a => a.Dog).FirstOrDefaultAsync(a => a.Id == msg.Id));
    }

    private async Task Handle(CreateAdoption msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var adoption = new Adoption
        {
            DogId = msg.DogId, ApplicantName = msg.ApplicantName,
            ApplicantEmail = msg.ApplicantEmail, ApplicantPhone = msg.ApplicantPhone,
            Type = msg.Type, Notes = msg.Notes
        };
        db.Adoptions.Add(adoption);
        await db.SaveChangesAsync();
        Sender.Tell(adoption);
    }

    private async Task Handle(UpdateAdoption msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
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
    }

    private async Task Handle(UpdateAdoptionStatus msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var adoption = await db.Adoptions.FindAsync(msg.Id);
        if (adoption is null) { Sender.Tell((Adoption?)null); return; }
        adoption.Status = msg.NewStatus;
        adoption.UpdatedAt = DateTime.UtcNow;
        if (msg.Notes is not null) adoption.Notes = msg.Notes;
        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(adoption.ApplicantEmail))
        {
            var emailSender = scope.ServiceProvider.GetService<IShelterEmailSender>();
            if (emailSender is not null)
                await emailSender.SendAsync(
                    adoption.ApplicantEmail,
                    $"Application update for {adoption.ApplicantName}",
                    $"Your adoption application status has been updated to: {msg.NewStatus}.");
        }

        Sender.Tell(adoption);
    }

    private async Task Handle(DeleteAdoption msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var adoption = await db.Adoptions.FindAsync(msg.Id);
        if (adoption is null) { Sender.Tell(false); return; }
        adoption.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        Sender.Tell(true);
    }

    private async Task Handle(GetDeletedAdoptions msg)
    {
        using var scope = _scopeFactory.CreateScope();
        Sender.Tell(await Db(scope).Adoptions.IgnoreQueryFilters()
            .Include(a => a.Dog)
            .Where(a => a.DeletedAt != null)
            .OrderByDescending(a => a.DeletedAt)
            .ToListAsync());
    }

    private async Task Handle(RestoreAdoption msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);
        var adoption = await db.Adoptions.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == msg.Id);
        if (adoption is null) { Sender.Tell(false); return; }
        adoption.DeletedAt = null;
        await db.SaveChangesAsync();
        Sender.Tell(true);
    }

    private async Task Handle(GetAdoptionConversionStats msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);

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
    }

    private async Task Handle(GetShelterStayStats msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = Db(scope);

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
    }
}
