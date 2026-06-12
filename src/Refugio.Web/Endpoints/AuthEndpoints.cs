using System.Globalization;
using System.Security.Claims;
using Akka.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Refugio.Actors;
using Refugio.Actors.Messages;
using Refugio.Application.Contracts;
using Refugio.Domain.Helpers;
using Refugio.Web.Helpers;

namespace Refugio.Web.Endpoints;

// Request records for the JSON auth endpoints consumed by the React SPA.
public record LoginRequest(string Email, string Password);
public record ChangePasswordJsonRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);

public static class AuthEndpoints
{
    /// <summary>JSON auth endpoints for the React SPA — mounted under /api.</summary>
    public static RouteGroupBuilder MapApiAuthEndpoints(this RouteGroupBuilder api)
    {
        // POST /api/auth/login — JSON in, JSON out, issues the same auth cookie.
        api.MapPost("/auth/login", async (LoginRequest body, HttpContext ctx, IActorRegistry actors, CancellationToken ct) =>
        {
            var v = await actors.Get<VolunteerActor>().AskFor<VolunteerDto>(new Login(body.Email, body.Password), ct);
            if (v is null) return Results.Unauthorized();
            var role = v.Role == Roles.Manager ? Roles.Manager : Roles.Volunteer;
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, v.Id.ToString()),
                new(ClaimTypes.Name, v.Name),
                new(ClaimTypes.Email, v.Email),
                new(ClaimTypes.Role, role),
            };
            await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
            return Results.Ok(new { id = v.Id, name = v.Name, email = v.Email, role });
        }).DisableAntiforgery();

        // GET /api/auth/me — returns current user from claims or 401.
        api.MapGet("/auth/me", (HttpContext ctx) =>
        {
            if (ctx.User.Identity?.IsAuthenticated != true) return Results.Unauthorized();
            return Results.Ok(new
            {
                id = int.Parse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)!),
                name = ctx.User.FindFirstValue(ClaimTypes.Name),
                email = ctx.User.FindFirstValue(ClaimTypes.Email),
                role = ctx.User.FindFirstValue(ClaimTypes.Role),
            });
        }).RequireAuthorization();

        // POST /api/auth/logout — clears cookie, returns 200.
        api.MapPost("/auth/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok();
        }).DisableAntiforgery();

        // POST /api/auth/change-password — JSON in, 200 or 400.
        api.MapPost("/auth/change-password", async (ChangePasswordJsonRequest body, HttpContext ctx, IActorRegistry actors, CancellationToken ct) =>
        {
            var id = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id is null) return Results.Unauthorized();
            if (string.IsNullOrEmpty(body.NewPassword) || body.NewPassword.Length < 6 || body.NewPassword != body.ConfirmPassword)
                return Results.BadRequest(new { error = "invalid" });
            var ok = await actors.Get<VolunteerActor>().AskRequired<bool>(
                new ChangePassword(int.Parse(id), body.CurrentPassword, body.NewPassword), ct);
            return ok ? Results.Ok(new { success = true }) : Results.BadRequest(new { error = "wrong_current" });
        }).RequireAuthorization().DisableAntiforgery();

        return api;
    }

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

        app.MapPost("/auth/login", async (HttpContext ctx, IActorRegistry actors, CancellationToken ct) =>
        {
            var form = await ctx.Request.ReadFormAsync();
            var email = FormReader.GetString(form, "email");
            var password = form["password"].ToString();
            var volunteer = await actors.Get<VolunteerActor>().AskFor<VolunteerDto>(new Login(email, password), ct);
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

            // Apply the volunteer's preferred UI language, if set and supported.
            if (volunteer.PreferredLanguage is { } lang && supportedCultures.Any(c => c.Name == lang))
            {
                ctx.Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(lang)),
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });
            }

            return Results.Redirect("/");
        }).DisableAntiforgery();

        app.MapGet("/auth/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/login");
        });

        app.MapPost("/auth/change-password", async (HttpContext ctx, IActorRegistry actors, CancellationToken ct) =>
        {
            var userIdClaim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim is null) return Results.Redirect("/login");
            var form = await ctx.Request.ReadFormAsync();
            var current = form["currentPassword"].ToString();
            var newPw = form["newPassword"].ToString();
            var confirm = form["confirmPassword"].ToString();
            if (string.IsNullOrEmpty(current) || string.IsNullOrEmpty(newPw) || newPw.Length < 6 || newPw != confirm)
                return Results.Redirect("/change-password?error=1");
            var ok = await actors.Get<VolunteerActor>().AskRequired<bool>(
                new ChangePassword(int.Parse(userIdClaim), current, newPw), ct);
            return Results.Redirect(ok ? "/change-password?success=1" : "/change-password?error=1");
        }).RequireAuthorization().DisableAntiforgery();

        return app;
    }
}
