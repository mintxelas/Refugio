using Refugio.Application.Contracts;
using Refugio.Application.Services;
using Refugio.Domain.Entities;
using Refugio.Domain.Helpers;
using Refugio.Tests.Helpers;

namespace Refugio.Tests.Services;

public class VolunteerServiceTests : ServiceTestBase
{
    private Task<T> Svc<T>(Func<IVolunteerService, Task<T>> action) => WithServiceAsync(action);

    private Task<Volunteer> SeedVolunteer(string name = "Alice", VolunteerStatus status = VolunteerStatus.Active,
        bool canLogin = false, string? password = null, string? preferredLanguage = null, DateTime? deletedAt = null)
        => SeedAsync(db =>
        {
            var volunteer = Volunteer.Register(name, $"{name.ToLower()}@test.com", null, Roles.Volunteer, null,
                canLogin, password, preferredLanguage, status);
            volunteer.DeletedAt = deletedAt;
            db.Volunteers.Add(volunteer);
            return volunteer;
        });

    [Fact]
    public async Task GetVolunteers_ReturnsEmpty_WhenNone()
    {
        Assert.Empty(await Svc(s => s.GetVolunteersAsync()));
    }

    [Fact]
    public async Task GetVolunteers_ReturnsAll()
    {
        await SeedVolunteer("Alice");
        await SeedVolunteer("Bob");
        Assert.Equal(2, (await Svc(s => s.GetVolunteersAsync())).Count);
    }

    [Fact]
    public async Task GetVolunteers_FiltersByStatus()
    {
        await SeedVolunteer("Alice", VolunteerStatus.Active);
        await SeedVolunteer("Bob", VolunteerStatus.Inactive);
        var result = await Svc(s => s.GetVolunteersAsync(VolunteerStatus.Active));
        Assert.Single(result);
        Assert.Equal("Alice", result[0].Name);
    }

    [Fact]
    public async Task GetVolunteer_ReturnsVolunteer_WhenFound()
    {
        var seeded = await SeedVolunteer("Carol");
        var result = await Svc(s => s.GetVolunteerAsync(seeded.Id));
        Assert.NotNull(result);
        Assert.Equal("Carol", result.Name);
    }

    [Fact]
    public async Task Register_NoLogin_DoesNotHashPassword()
    {
        var result = await Svc(s => s.RegisterAsync(
            new CreateVolunteerRequest("Dave", "dave@test.com", "555-0001", Roles.Volunteer, null, CanLogin: false)));
        Assert.Equal("Dave", result.Name);
        Assert.False(result.CanLogin);
        Assert.True(result.Id > 0);
        var stored = await ReadDirectAsync<Volunteer>(result.Id);
        Assert.Null(stored!.PasswordHash);
    }

    [Fact]
    public async Task Register_WithLogin_HashesPassword()
    {
        var result = await Svc(s => s.RegisterAsync(
            new CreateVolunteerRequest("Eve", "eve@test.com", null, Roles.Manager, null, CanLogin: true, Password: "secret123")));
        Assert.True(result.CanLogin);
        var stored = await ReadDirectAsync<Volunteer>(result.Id);
        Assert.NotNull(stored!.PasswordHash);
        Assert.True(PasswordHelper.Verify("secret123", stored.PasswordHash!));
    }

    [Fact]
    public async Task Register_WithLogin_EmptyPassword_NoHash()
    {
        var result = await Svc(s => s.RegisterAsync(
            new CreateVolunteerRequest("Frank", "frank@test.com", null, Roles.Volunteer, null, CanLogin: true, Password: "")));
        var stored = await ReadDirectAsync<Volunteer>(result.Id);
        Assert.Null(stored!.PasswordHash);
    }

    [Fact]
    public async Task VolunteerDto_NeverExposesPasswordHash()
    {
        // DTO type has no PasswordHash member at all — the API cannot leak it.
        Assert.Null(typeof(VolunteerDto).GetProperty("PasswordHash"));
    }

    [Fact]
    public async Task Update_UpdatesFields_WhenFound()
    {
        var seeded = await SeedVolunteer("Old");
        var result = await Svc(s => s.UpdateAsync(
            new UpdateVolunteerRequest(seeded.Id, "New", "new@test.com", "555-9999", Roles.Volunteer, "note", true, VolunteerStatus.Active, "newpass")));
        Assert.NotNull(result);
        Assert.Equal("New", result.Name);
        Assert.Equal("new@test.com", result.Email);
        Assert.True(result.CanLogin);
        var stored = await ReadDirectAsync<Volunteer>(seeded.Id);
        Assert.NotNull(stored!.PasswordHash);
    }

    [Fact]
    public async Task Update_ReturnsNull_WhenNotFound()
    {
        Assert.Null(await Svc(s => s.UpdateAsync(
            new UpdateVolunteerRequest(99999, "X", "x@x.com", null, "Y", null, false, VolunteerStatus.Active))));
    }

    [Fact]
    public async Task Update_ClearsPassword_WhenCanLoginFalse()
    {
        var seeded = await SeedVolunteer("WithLogin", canLogin: true, password: "oldpw");
        var result = await Svc(s => s.UpdateAsync(
            new UpdateVolunteerRequest(seeded.Id, "WithLogin", seeded.Email, null, Roles.Volunteer, null, false, VolunteerStatus.Active)));
        Assert.NotNull(result);
        Assert.False(result.CanLogin);
        var stored = await ReadDirectAsync<Volunteer>(seeded.Id);
        Assert.Null(stored!.PasswordHash);
    }

    [Fact]
    public async Task Update_NoNewPassword_KeepsExistingHash()
    {
        var seeded = await SeedVolunteer("Keeper", canLogin: true, password: "keep123");
        var oldHash = seeded.PasswordHash;
        await Svc(s => s.UpdateAsync(
            new UpdateVolunteerRequest(seeded.Id, "Keeper", seeded.Email, null, Roles.Volunteer, null, true, VolunteerStatus.Active, NewPassword: null)));
        var stored = await ReadDirectAsync<Volunteer>(seeded.Id);
        Assert.Equal(oldHash, stored!.PasswordHash);
    }

    [Fact]
    public async Task Update_PersistsPreferredLanguage_WhenCanLogin()
    {
        var seeded = await SeedVolunteer("Lang", canLogin: true, password: "pw");
        var result = await Svc(s => s.UpdateAsync(
            new UpdateVolunteerRequest(seeded.Id, "Lang", seeded.Email, null, Roles.Volunteer, null, true, VolunteerStatus.Active, NewPassword: null, PreferredLanguage: "es-ES")));
        Assert.NotNull(result);
        Assert.Equal("es-ES", result.PreferredLanguage);
    }

    [Fact]
    public async Task Update_ClearsPreferredLanguage_WhenCanLoginFalse()
    {
        var seeded = await SeedVolunteer("HadLang", canLogin: true, password: "pw", preferredLanguage: "pt-BR");
        var result = await Svc(s => s.UpdateAsync(
            new UpdateVolunteerRequest(seeded.Id, "HadLang", seeded.Email, null, Roles.Volunteer, null, false, VolunteerStatus.Active, PreferredLanguage: "pt-BR")));
        Assert.NotNull(result);
        Assert.False(result.CanLogin);
        Assert.Null(result.PreferredLanguage);
    }

    [Fact]
    public async Task ChangeStatus_UpdatesStatus_WhenFound()
    {
        var seeded = await SeedVolunteer("G", VolunteerStatus.Active);
        var result = await Svc(s => s.ChangeStatusAsync(seeded.Id, VolunteerStatus.Inactive));
        Assert.NotNull(result);
        Assert.Equal(VolunteerStatus.Inactive, result.Status);
    }

    [Fact]
    public async Task ChangeStatus_ReturnsNull_WhenNotFound()
    {
        Assert.Null(await Svc(s => s.ChangeStatusAsync(99999, VolunteerStatus.Inactive)));
    }

    [Fact]
    public async Task Delete_ReturnsTrue_WhenFound()
    {
        var seeded = await SeedVolunteer("Del");
        Assert.True(await Svc(s => s.DeleteAsync(seeded.Id)));
    }

    [Fact]
    public async Task Delete_ReturnsFalse_WhenNotFound()
    {
        Assert.False(await Svc(s => s.DeleteAsync(99999)));
    }

    [Fact]
    public async Task Login_ReturnsVolunteer_WhenCredentialsValid()
    {
        await SeedVolunteer("Hanna", canLogin: true, password: "pass1234");
        var result = await Svc(s => s.LoginAsync("hanna@test.com", "pass1234"));
        Assert.NotNull(result);
        Assert.Equal("Hanna", result.Name);
    }

    [Fact]
    public async Task Login_ReturnsNull_WhenWrongPassword()
    {
        await SeedVolunteer("Ivan", canLogin: true, password: "correct");
        Assert.Null(await Svc(s => s.LoginAsync("ivan@test.com", "wrong")));
    }

    [Fact]
    public async Task Login_ReturnsNull_WhenUserNotFound()
    {
        Assert.Null(await Svc(s => s.LoginAsync("nobody@test.com", "pass")));
    }

    [Fact]
    public async Task Login_ReturnsNull_WhenCanLoginFalse()
    {
        await SeedVolunteer("NoLogin", canLogin: false);
        Assert.Null(await Svc(s => s.LoginAsync("nologin@test.com", "any")));
    }

    [Fact]
    public async Task ChangePassword_ReturnsTrue_WhenValid()
    {
        var seeded = await SeedVolunteer("Jake", canLogin: true, password: "oldPass");
        Assert.True(await Svc(s => s.ChangePasswordAsync(seeded.Id, "oldPass", "newPass")));
        Assert.NotNull(await Svc(s => s.LoginAsync("jake@test.com", "newPass")));
    }

    [Fact]
    public async Task ChangePassword_ReturnsFalse_WhenWrongCurrent()
    {
        var seeded = await SeedVolunteer("Kim", canLogin: true, password: "correct");
        Assert.False(await Svc(s => s.ChangePasswordAsync(seeded.Id, "wrong", "newPass")));
    }

    [Fact]
    public async Task ChangePassword_ReturnsFalse_WhenNoHash()
    {
        var seeded = await SeedVolunteer("NoPass", canLogin: false);
        Assert.False(await Svc(s => s.ChangePasswordAsync(seeded.Id, "any", "new")));
    }

    [Fact]
    public async Task SetPhoto_ReturnsTrue_AndSetsUrl_WhenFound()
    {
        var seeded = await SeedVolunteer("Pic");
        Assert.True(await Svc(s => s.SetPhotoAsync(seeded.Id, "/volunteers/1.jpg")));
        var volunteer = await Svc(s => s.GetVolunteerAsync(seeded.Id));
        Assert.Equal("/volunteers/1.jpg", volunteer!.PhotoUrl);
    }

    [Fact]
    public async Task SetPhoto_ReturnsFalse_WhenNotFound()
    {
        Assert.False(await Svc(s => s.SetPhotoAsync(99999, "/volunteers/x.jpg")));
    }

    [Fact]
    public async Task GetDeleted_ReturnsOnlyDeleted()
    {
        await SeedVolunteer("ActiveVol");
        await SeedVolunteer("GoneVol", deletedAt: DateTime.UtcNow);
        var deleted = await Svc(s => s.GetDeletedAsync());
        Assert.Single(deleted);
        Assert.Equal("GoneVol", deleted[0].Name);
    }

    [Fact]
    public async Task Restore_ReturnsTrue_AndAppearsInActiveQuery()
    {
        var seeded = await SeedVolunteer("ReviveVol", deletedAt: DateTime.UtcNow);
        Assert.True(await Svc(s => s.RestoreAsync(seeded.Id)));
        var volunteer = await Svc(s => s.GetVolunteerAsync(seeded.Id));
        Assert.NotNull(volunteer);
        Assert.Null(volunteer.DeletedAt);
    }

    [Fact]
    public async Task Restore_ReturnsFalse_WhenNotFound()
    {
        Assert.False(await Svc(s => s.RestoreAsync(99999)));
    }
}
