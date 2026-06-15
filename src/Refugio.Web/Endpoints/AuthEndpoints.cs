using System.Security.Claims;
using Akka.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Refugio.Actors;
using Refugio.Actors.Messages;
using Refugio.Application.Contracts;
using Refugio.Domain.Helpers;

namespace Refugio.Web.Endpoints;

public record LoginRequest(string Email, string Password);
public record ChangePasswordJsonRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapApiAuthEndpoints(this RouteGroupBuilder api)
    {
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
        }).AllowAnonymous().DisableAntiforgery();

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

        api.MapPost("/auth/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok();
        }).AllowAnonymous().DisableAntiforgery();

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
}
