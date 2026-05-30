namespace Refugio.Domain.Helpers;

/// <summary>
/// Canonical role names. <see cref="Volunteer.Role"/> stores one of these; the
/// "Manager" authorization policy requires <see cref="Manager"/>. Treat as the
/// single source of truth — never inline the literal strings.
/// </summary>
public static class Roles
{
    public const string Manager = "Manager";
    public const string Volunteer = "Volunteer";
}
