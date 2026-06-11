using Refugio.Domain.Entities;

namespace Refugio.Application.Contracts;

/// <summary>Volunteer as exposed by the API. Never carries the password hash.</summary>
public record VolunteerDto(
    int Id, string Name, string Email, string? Phone, string Role, VolunteerStatus Status,
    DateTime JoinDate, string? Notes, bool CanLogin, string? PreferredLanguage,
    string? PhotoUrl, DateTime? DeletedAt);

public record CreateVolunteerRequest(
    string Name, string Email, string? Phone, string Role, string? Notes,
    bool CanLogin = false, string? Password = null, string? PreferredLanguage = null);

public record UpdateVolunteerRequest(
    int Id, string Name, string Email, string? Phone, string Role, string? Notes,
    bool CanLogin, VolunteerStatus Status, string? NewPassword = null, string? PreferredLanguage = null);

public record UpdateVolunteerStatusRequest(int Id, VolunteerStatus Status);
