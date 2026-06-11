using Refugio.Domain.Entities;
using Refugio.Domain.Events;
using Refugio.Domain.Helpers;

namespace Refugio.Tests.Domain;

public class AdoptionBehaviorTests
{
    private static Adoption NewAdoption(string? email = "a@t.com")
        => Adoption.Submit(1, "Alice", email, null, AdoptionType.Adoption, null);

    [Fact]
    public void Submit_DefaultsToApplied_WithoutUpdatedAt()
    {
        var adoption = NewAdoption();
        Assert.Equal(AdoptionStatus.Applied, adoption.Status);
        Assert.Null(adoption.UpdatedAt);
        Assert.Empty(adoption.DequeueDomainEvents());
    }

    [Fact]
    public void ChangeStatus_SetsStatusAndUpdatedAt_AndRaisesEvent()
    {
        var adoption = NewAdoption();
        adoption.ChangeStatus(AdoptionStatus.Interview);

        Assert.Equal(AdoptionStatus.Interview, adoption.Status);
        Assert.NotNull(adoption.UpdatedAt);

        var events = adoption.DequeueDomainEvents();
        var statusChanged = Assert.IsType<AdoptionStatusChanged>(Assert.Single(events));
        Assert.Equal("Alice", statusChanged.ApplicantName);
        Assert.Equal("a@t.com", statusChanged.ApplicantEmail);
        Assert.Equal(AdoptionStatus.Interview, statusChanged.NewStatus);
    }

    [Fact]
    public void ChangeStatus_KeepsNotes_WhenNullPassed()
    {
        var adoption = Adoption.Submit(1, "Alice", null, null, AdoptionType.Adoption, "original");
        adoption.ChangeStatus(AdoptionStatus.Interview, null);
        Assert.Equal("original", adoption.Notes);
    }

    [Fact]
    public void UpdateDetails_DoesNotRaiseEvent()
    {
        var adoption = NewAdoption();
        adoption.UpdateDetails("Bob", "b@t.com", null, AdoptionType.Foster, AdoptionStatus.Approved, "edited");
        Assert.Equal(AdoptionStatus.Approved, adoption.Status);
        Assert.Empty(adoption.DequeueDomainEvents());
    }

    [Fact]
    public void DequeueDomainEvents_EmptiesTheQueue()
    {
        var adoption = NewAdoption();
        adoption.ChangeStatus(AdoptionStatus.Interview);
        Assert.Single(adoption.DequeueDomainEvents());
        Assert.Empty(adoption.DequeueDomainEvents());
    }

    [Theory]
    [InlineData(AdoptionStatus.Applied, AdoptionStatus.Interview)]
    [InlineData(AdoptionStatus.Interview, AdoptionStatus.HomeCheck)]
    [InlineData(AdoptionStatus.HomeCheck, AdoptionStatus.Approved)]
    [InlineData(AdoptionStatus.Approved, AdoptionStatus.Finalized)]
    [InlineData(AdoptionStatus.Finalized, AdoptionStatus.Finalized)]
    [InlineData(AdoptionStatus.Rejected, AdoptionStatus.Rejected)]
    public void NextStatus_FollowsPipeline(AdoptionStatus current, AdoptionStatus expected)
    {
        var adoption = Adoption.Submit(1, "X", null, null, AdoptionType.Adoption, null, current);
        Assert.Equal(expected, adoption.NextStatus());
    }
}

public class VolunteerBehaviorTests
{
    [Fact]
    public void Register_WithLogin_HashesPassword()
    {
        var volunteer = Volunteer.Register("Eve", "eve@t.com", null, "Admin", null, canLogin: true, password: "secret123");
        Assert.True(volunteer.CanLogin);
        Assert.True(volunteer.VerifyPassword("secret123"));
        Assert.False(volunteer.VerifyPassword("wrong"));
    }

    [Fact]
    public void Register_WithoutLogin_HasNoHashOrLanguage()
    {
        var volunteer = Volunteer.Register("Dave", "dave@t.com", null, "Driver", null,
            canLogin: false, password: "ignored", preferredLanguage: "es-ES");
        Assert.False(volunteer.CanLogin);
        Assert.Null(volunteer.PasswordHash);
        Assert.Null(volunteer.PreferredLanguage);
    }

    [Fact]
    public void Update_DisablingLogin_ClearsHashAndLanguage()
    {
        var volunteer = Volunteer.Register("Eve", "eve@t.com", null, "Admin", null,
            canLogin: true, password: "secret123", preferredLanguage: "pt-BR");
        volunteer.Update("Eve", "eve@t.com", null, "Admin", null, VolunteerStatus.Active, canLogin: false);
        Assert.Null(volunteer.PasswordHash);
        Assert.Null(volunteer.PreferredLanguage);
    }

    [Fact]
    public void ChangePassword_FailsOnWrongCurrent_SucceedsOnRight()
    {
        var volunteer = Volunteer.Register("Eve", "eve@t.com", null, "Admin", null, canLogin: true, password: "old");
        Assert.False(volunteer.ChangePassword("wrong", "new"));
        Assert.True(volunteer.VerifyPassword("old"));
        Assert.True(volunteer.ChangePassword("old", "new"));
        Assert.True(volunteer.VerifyPassword("new"));
    }

    [Fact]
    public void EnableLogin_SetsCanLoginAndHash()
    {
        var volunteer = Volunteer.Register("Elena", "elena@t.com", null, Roles.Manager, null);
        volunteer.EnableLogin("shelter123");
        Assert.True(volunteer.CanLogin);
        Assert.True(volunteer.VerifyPassword("shelter123"));
    }
}

public class MiscBehaviorTests
{
    [Fact]
    public void Medication_Deactivate_SetsIsActiveFalse()
    {
        var medication = Medication.Create(1, "Pill", "1mg", "Daily", DateTime.UtcNow, null);
        Assert.True(medication.IsActive);
        medication.Deactivate();
        Assert.False(medication.IsActive);
    }

    [Fact]
    public void Entity_Restore_ClearsDeletedAt()
    {
        var dog = Dog.CheckIn("Rex", "Lab", 12, "M", 10m);
        dog.DeletedAt = DateTime.UtcNow;
        dog.Restore();
        Assert.Null(dog.DeletedAt);
    }

    [Fact]
    public void ShelterTask_Complete_SetsFlag()
    {
        var task = ShelterTask.Create("Feed", DateTime.UtcNow);
        Assert.False(task.IsCompleted);
        task.Complete();
        Assert.True(task.IsCompleted);
    }

    [Fact]
    public void Dog_CheckIn_DefaultsToAvailable_WithArrivalNow()
    {
        var dog = Dog.CheckIn("Rex", "Lab", 12, "M", 10m);
        Assert.Equal(DogStatus.Available, dog.Status);
        Assert.True((DateTime.UtcNow - dog.ArrivalDate).TotalMinutes < 1);
    }
}
