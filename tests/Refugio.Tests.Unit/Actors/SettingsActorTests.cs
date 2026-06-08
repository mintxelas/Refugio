using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Application.Actors;
using Refugio.Application.Messages;
using Refugio.Domain.Entities;
using Refugio.Tests.Helpers;

namespace Refugio.Tests.Actors;

public class SettingsActorTests : ActorTestBase
{
    private readonly IActorRef _actor;

    public SettingsActorTests()
    {
        _actor = Sys.ActorOf(Props.Create(() => new SettingsActor(_sf)));
    }

    [Fact]
    public async Task GetSettings_ReturnsSeededOrCreatedRow()
    {
        // Empty DB — GetOrCreate path should create a default row
        var result = await _actor.Ask<ShelterSettings>(new GetSettings(), TimeSpan.FromSeconds(5));
        Assert.NotNull(result);
        Assert.Equal("Haven Sanctuary", result.Name);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task UpdateSettings_PersistsNameAndPhrase()
    {
        // First ensure a row exists
        await _actor.Ask<ShelterSettings>(new GetSettings(), TimeSpan.FromSeconds(5));

        var updated = await _actor.Ask<ShelterSettings>(
            new UpdateSettings("New Name", "New Phrase"),
            TimeSpan.FromSeconds(5));

        Assert.NotNull(updated);
        Assert.Equal("New Name", updated.Name);
        Assert.Equal("New Phrase", updated.Phrase);

        // Re-read to confirm persistence
        var reread = await _actor.Ask<ShelterSettings>(new GetSettings(), TimeSpan.FromSeconds(5));
        Assert.Equal("New Name", reread.Name);
        Assert.Equal("New Phrase", reread.Phrase);
    }

    [Fact]
    public async Task UpdateSettings_AllowsNullPhrase()
    {
        await _actor.Ask<ShelterSettings>(new GetSettings(), TimeSpan.FromSeconds(5));

        var updated = await _actor.Ask<ShelterSettings>(
            new UpdateSettings("Some Shelter", null),
            TimeSpan.FromSeconds(5));

        Assert.NotNull(updated);
        Assert.Equal("Some Shelter", updated.Name);
        Assert.Null(updated.Phrase);
    }

    [Fact]
    public async Task UpdateSettingsLogo_PersistsUrl()
    {
        await _actor.Ask<ShelterSettings>(new GetSettings(), TimeSpan.FromSeconds(5));

        var ok = await _actor.Ask<bool>(
            new UpdateSettingsLogo("/branding/logo.png"),
            TimeSpan.FromSeconds(5));
        Assert.True(ok);

        var reread = await _actor.Ask<ShelterSettings>(new GetSettings(), TimeSpan.FromSeconds(5));
        Assert.Equal("/branding/logo.png", reread.LogoUrl);
    }

    [Fact]
    public async Task SingleRow_NeverDuplicated()
    {
        // Multiple GetSettings / UpdateSettings calls should keep exactly one row
        await _actor.Ask<ShelterSettings>(new GetSettings(), TimeSpan.FromSeconds(5));
        await _actor.Ask<ShelterSettings>(new GetSettings(), TimeSpan.FromSeconds(5));
        await _actor.Ask<ShelterSettings>(new UpdateSettings("A", "B"), TimeSpan.FromSeconds(5));
        await _actor.Ask<ShelterSettings>(new GetSettings(), TimeSpan.FromSeconds(5));

        using var scope = _sf.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Refugio.Infrastructure.Data.ShelterDbContext>();
        var count = db.Settings.Count();
        Assert.Equal(1, count);
    }
}
