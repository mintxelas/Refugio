using Microsoft.Extensions.DependencyInjection;
using Refugio.Application.Contracts;
using Refugio.Application.Services;
using Refugio.Infrastructure.Data;
using Refugio.Tests.Helpers;

namespace Refugio.Tests.Services;

public class SettingsServiceTests : ServiceTestBase
{
    private Task<T> Svc<T>(Func<ISettingsService, Task<T>> action) => WithServiceAsync(action);

    [Fact]
    public async Task Get_CreatesDefaultRow_WhenEmpty()
    {
        var result = await Svc(s => s.GetAsync());
        Assert.NotNull(result);
        Assert.Equal("Haven Sanctuary", result.Name);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task Update_PersistsNameAndPhrase()
    {
        await Svc(s => s.GetAsync());

        var updated = await Svc(s => s.UpdateAsync(new UpdateSettingsRequest("New Name", "New Phrase")));
        Assert.Equal("New Name", updated.Name);
        Assert.Equal("New Phrase", updated.Phrase);

        var reread = await Svc(s => s.GetAsync());
        Assert.Equal("New Name", reread.Name);
        Assert.Equal("New Phrase", reread.Phrase);
    }

    [Fact]
    public async Task Update_AllowsNullPhrase()
    {
        await Svc(s => s.GetAsync());
        var updated = await Svc(s => s.UpdateAsync(new UpdateSettingsRequest("Some Shelter", null)));
        Assert.Equal("Some Shelter", updated.Name);
        Assert.Null(updated.Phrase);
    }

    [Fact]
    public async Task SetLogo_PersistsUrl()
    {
        await Svc(s => s.GetAsync());
        Assert.True(await Svc(s => s.SetLogoAsync("/branding/logo.png")));
        var reread = await Svc(s => s.GetAsync());
        Assert.Equal("/branding/logo.png", reread.LogoUrl);
    }

    [Fact]
    public async Task SingleRow_NeverDuplicated()
    {
        await Svc(s => s.GetAsync());
        await Svc(s => s.GetAsync());
        await Svc(s => s.UpdateAsync(new UpdateSettingsRequest("A", "B")));
        await Svc(s => s.GetAsync());

        using var scope = ScopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();
        Assert.Equal(1, db.Settings.Count());
    }
}
