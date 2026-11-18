using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Refugio.Domain.Entities;
using Refugio.Domain.Helpers;
using Refugio.Infrastructure.Data;

namespace Refugio.Web.Helpers;

/// <summary>
/// Ties an auth cookie to the volunteer's current state. The cookie carries a stamp derived
/// from the password hash; every request re-checks that the volunteer still exists, can log in,
/// is Active, has the same role, and has not had their password changed. Otherwise the session dies.
/// </summary>
public static class VolunteerSession
{
    public const string StampClaim = "refugio:stamp";

    public static async Task AddStampAsync(CookieSigningInContext ctx)
    {
        var identity = (ClaimsIdentity)ctx.Principal!.Identity!;
        identity.TryRemoveClaim(identity.FindFirst(StampClaim));
        var state = await LoadAsync(ctx.HttpContext, ctx.Principal);
        identity.AddClaim(new Claim(StampClaim, Stamp(state?.PasswordHash)));
    }

    // ponytail: one indexed SQLite lookup per authenticated request; cache with a short TTL if it ever shows up in profiles.
    public static async Task ValidateAsync(CookieValidatePrincipalContext ctx)
    {
        var principal = ctx.Principal!;
        var state = await LoadAsync(ctx.HttpContext, principal);
        if (state is not null && state.CanLogin && state.Status == VolunteerStatus.Active
            && RoleClaimFor(state.Role) == principal.FindFirstValue(ClaimTypes.Role)
            && Stamp(state.PasswordHash) == principal.FindFirstValue(StampClaim))
            return;

        ctx.RejectPrincipal();
        await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public static string RoleClaimFor(string role) => role == Roles.Manager ? Roles.Manager : Roles.Volunteer;

    private record State(bool CanLogin, VolunteerStatus Status, string Role, string? PasswordHash);

    private static Task<State?> LoadAsync(HttpContext http, ClaimsPrincipal principal)
    {
        if (!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            return Task.FromResult<State?>(null);
        var db = http.RequestServices.GetRequiredService<ShelterDbContext>();
        return db.Volunteers.AsNoTracking()
            .Where(v => v.Id == id)
            .Select(v => new State(v.CanLogin, v.Status, v.Role, v.PasswordHash))
            .FirstOrDefaultAsync();
    }

    private static string Stamp(string? passwordHash) => passwordHash is null
        ? ""
        : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash)))[..16];
}
