using Akka.Actor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Application.Messages;
using Refugio.Domain.Entities;
using Refugio.Infrastructure.Data;

namespace Refugio.Application.Actors;

public class SettingsActor : ShelterActorBase
{
    public SettingsActor(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
        ReceiveAsync<GetSettings>(Handle);
        ReceiveAsync<UpdateSettings>(Handle);
        ReceiveAsync<UpdateSettingsLogo>(Handle);
    }

    private Task Handle(GetSettings msg)
    {
        var sender = Sender;
        return WithDb(async db => sender.Tell(await GetOrCreate(db)));
    }

    private Task Handle(UpdateSettings msg)
    {
        var sender = Sender;
        return WithDb(async db =>
        {
            var s = await GetOrCreate(db);
            s.Name = msg.Name;
            s.Phrase = msg.Phrase;
            await db.SaveChangesAsync();
            sender.Tell(s);
        });
    }

    private Task Handle(UpdateSettingsLogo msg)
    {
        var sender = Sender;
        return WithDb(async db =>
        {
            var s = await GetOrCreate(db);
            s.LogoUrl = msg.LogoUrl;
            await db.SaveChangesAsync();
            sender.Tell(true);
        });
    }

    private static async Task<ShelterSettings> GetOrCreate(ShelterDbContext db)
    {
        var s = await db.Settings.FirstOrDefaultAsync();
        if (s is null)
        {
            s = new ShelterSettings { Name = "Haven Sanctuary", Phrase = "City Main Branch" };
            db.Settings.Add(s);
            await db.SaveChangesAsync();
        }
        return s;
    }
}
