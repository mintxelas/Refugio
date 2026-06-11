using Refugio.Domain.Common;
using Refugio.Domain.Helpers;

namespace Refugio.Domain.Entities;

/// <summary>
/// Aggregate root for a shelter volunteer, including the optional login credentials.
/// Credential rules live here: no login → no password hash and no preferred language.
/// </summary>
public class Volunteer : Entity, IAggregateRoot
{
    public string Name { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string? Phone { get; private set; }
    public string Role { get; private set; } = "";
    public VolunteerStatus Status { get; private set; } = VolunteerStatus.Active;
    public DateTime JoinDate { get; private set; } = DateTime.UtcNow;
    public string? Notes { get; private set; }
    public bool CanLogin { get; private set; }
    public string? PasswordHash { get; private set; }
    public string? PreferredLanguage { get; private set; }
    public string? PhotoUrl { get; private set; }

    private Volunteer() { }

    public static Volunteer Register(
        string name, string email, string? phone, string role, string? notes,
        bool canLogin = false, string? password = null, string? preferredLanguage = null,
        VolunteerStatus status = VolunteerStatus.Active, DateTime? joinDate = null)
    {
        var volunteer = new Volunteer
        {
            Name = name,
            Email = email,
            Phone = phone,
            Role = role,
            Notes = notes,
            Status = status,
            JoinDate = joinDate ?? DateTime.UtcNow
        };
        volunteer.SetCredentials(canLogin, password, preferredLanguage);
        return volunteer;
    }

    public void Update(
        string name, string email, string? phone, string role, string? notes,
        VolunteerStatus status, bool canLogin, string? newPassword = null, string? preferredLanguage = null)
    {
        Name = name;
        Email = email;
        Phone = phone;
        Role = role;
        Notes = notes;
        Status = status;
        CanLogin = canLogin;
        PreferredLanguage = canLogin ? preferredLanguage : null;
        if (canLogin && !string.IsNullOrWhiteSpace(newPassword))
            PasswordHash = PasswordHelper.Hash(newPassword);
        else if (!canLogin)
            PasswordHash = null;
    }

    public void ChangeStatus(VolunteerStatus status) => Status = status;

    public void ChangeRole(string role) => Role = role;

    public void SetPhotoUrl(string? photoUrl) => PhotoUrl = photoUrl;

    public void EnableLogin(string password)
    {
        CanLogin = true;
        PasswordHash = PasswordHelper.Hash(password);
    }

    public bool VerifyPassword(string password)
        => PasswordHash is not null && PasswordHelper.Verify(password, PasswordHash);

    /// <summary>Changes the password only when the current one verifies.</summary>
    public bool ChangePassword(string currentPassword, string newPassword)
    {
        if (!VerifyPassword(currentPassword)) return false;
        PasswordHash = PasswordHelper.Hash(newPassword);
        return true;
    }

    private void SetCredentials(bool canLogin, string? password, string? preferredLanguage)
    {
        CanLogin = canLogin;
        PreferredLanguage = canLogin ? preferredLanguage : null;
        PasswordHash = canLogin && !string.IsNullOrWhiteSpace(password)
            ? PasswordHelper.Hash(password)
            : null;
    }
}

public enum VolunteerStatus { Active, Inactive, Pending }
