using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Refugio.Application.Messages;
using Refugio.Application.Services;
using Refugio.Domain.Entities;
using Refugio.Domain.Helpers;
using Refugio.Web.Helpers;

namespace Refugio.Web.Endpoints;

public static class AuthEndpoints
{
    public static WebApplication MapAuthEndpoints(this WebApplication app, IReadOnlyList<CultureInfo> supportedCultures)
    {
        // Language switcher — only honors a culture the app actually supports.
        app.MapGet("/set-language", (string? culture, string? returnUrl, HttpContext ctx) =>
        {
            if (culture is not null && supportedCultures.Any(c => c.Name == culture))
            {
                ctx.Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });
            }
            return Results.Redirect(returnUrl ?? "/");
        });

        app.MapPost("/auth/login", async (HttpContext ctx, ShelterActorService actors) =>
        {
            var form = await ctx.Request.ReadFormAsync();
            var email = FormReader.GetString(form, "email");
            var password = form["password"].ToString();
            var volunteer = await actors.Ask<Volunteer?>(new LoginVolunteer(email, password));
            if (volunteer is null) return Results.Redirect("/login?error=1");
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, volunteer.Id.ToString()),
                new(ClaimTypes.Name, volunteer.Name),
                new(ClaimTypes.Email, volunteer.Email),
                new(ClaimTypes.Role, volunteer.Role == Roles.Manager ? Roles.Manager : Roles.Volunteer),
            };
            await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
            return Results.Redirect("/");
        }).DisableAntiforgery();

        app.MapGet("/auth/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/login");
        });

        app.MapPost("/auth/change-password", async (HttpContext ctx, ShelterActorService actors) =>
        {
            var userIdClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim is null) return Results.Redirect("/login");
            var form = await ctx.Request.ReadFormAsync();
            var current = form["currentPassword"].ToString();
            var newPw = form["newPassword"].ToString();
            var confirm = form["confirmPassword"].ToString();
            if (string.IsNullOrEmpty(current) || string.IsNullOrEmpty(newPw) || newPw.Length < 6 || newPw != confirm)
                return Results.Redirect("/change-password?error=1");
            var ok = await actors.Ask<bool>(new ChangeVolunteerPassword(int.Parse(userIdClaim), current, newPw));
            return Results.Redirect(ok ? "/change-password?success=1" : "/change-password?error=1");
        }).RequireAuthorization().DisableAntiforgery();

        return app;
    }
}
