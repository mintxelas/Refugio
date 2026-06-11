using Refugio.Application.Contracts;
using Refugio.Application.Queries;
using Refugio.Application.Services;
using Refugio.Domain.Entities;

namespace Refugio.Web.Endpoints;

public static class AdoptionEndpoints
{
    public static RouteGroupBuilder MapAdoptionEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/adoptions", async (AdoptionStatus? status, IAdoptionService adoptions) =>
            Results.Ok(await adoptions.GetAdoptionsAsync(status)));

        api.MapGet("/adoptions/paged", async (AdoptionStatus? status, int? page, int? pageSize, IAdoptionService adoptions) =>
            Results.Ok(await adoptions.GetAdoptionsPagedAsync(status, page ?? 1, pageSize ?? 25)));

        api.MapGet("/adoptions/deleted", async (IAdoptionService adoptions) =>
            Results.Ok(await adoptions.GetDeletedAsync())).RequireAuthorization("Manager");

        api.MapGet("/adoptions/deleted/{id:int}", async (int id, IAdoptionService adoptions) =>
        {
            var adoption = await adoptions.GetDeletedByIdAsync(id);
            return adoption is null ? Results.NotFound() : Results.Ok(adoption);
        }).RequireAuthorization("Manager");

        api.MapGet("/adoptions/{id:int}", async (int id, IAdoptionService adoptions) =>
        {
            var adoption = await adoptions.GetAdoptionAsync(id);
            return adoption is null ? Results.NotFound() : Results.Ok(adoption);
        });

        api.MapPost("/adoptions", async (CreateAdoptionRequest request, IAdoptionService adoptions) =>
        {
            var adoption = await adoptions.SubmitAsync(request);
            return Results.Created($"/api/adoptions/{adoption.Id}", adoption);
        });

        api.MapPut("/adoptions/{id:int}", async (int id, UpdateAdoptionRequest request, IAdoptionService adoptions) =>
        {
            var adoption = await adoptions.UpdateAsync(request with { Id = id });
            return adoption is null ? Results.NotFound() : Results.Ok(adoption);
        });

        api.MapPut("/adoptions/{id:int}/status", async (int id, UpdateAdoptionStatusRequest request, IAdoptionService adoptions) =>
        {
            var adoption = await adoptions.ChangeStatusAsync(request with { Id = id });
            return adoption is null ? Results.NotFound() : Results.Ok(adoption);
        });

        api.MapDelete("/adoptions/{id:int}", async (int id, IAdoptionService adoptions) =>
            await adoptions.DeleteAsync(id) ? Results.NoContent() : Results.NotFound());

        api.MapPost("/adoptions/{id:int}/delete", async (int id, IAdoptionService adoptions) =>
        {
            await adoptions.DeleteAsync(id);
            return Results.Redirect("/adoptions");
        }).RequireAuthorization("Manager");

        api.MapPost("/adoptions/{id:int}/restore", async (int id, IAdoptionService adoptions) =>
        {
            await adoptions.RestoreAsync(id);
            return Results.Redirect("/admin/deleted?tab=adoptions");
        }).RequireAuthorization("Manager");

        api.MapPost("/adoptions/{id:int}/purge", async (int id, IAdoptionService adoptions) =>
        {
            await adoptions.PurgeAsync(id);
            return Results.Redirect("/admin/deleted?tab=adoptions");
        }).RequireAuthorization("Manager");

        api.MapPost("/adoptions/{id:int}/advance", async (int id, IAdoptionService adoptions) =>
        {
            await adoptions.AdvanceAsync(id);
            return Results.Redirect("/adoptions");
        }).RequireAuthorization();

        api.MapPost("/adoptions/{id:int}/reject", async (int id, IAdoptionService adoptions) =>
        {
            await adoptions.ChangeStatusAsync(new UpdateAdoptionStatusRequest(id, AdoptionStatus.Rejected, null));
            return Results.Redirect("/adoptions");
        }).RequireAuthorization();

        // Reports (sourced from adoption data)
        api.MapGet("/reports/adoption-conversion", async (int? year, IAdoptionQueries reports) =>
            Results.Ok(await reports.GetConversionStatsAsync(year ?? DateTime.UtcNow.Year)))
            .RequireAuthorization();

        api.MapGet("/reports/shelter-stay", async (IAdoptionQueries reports) =>
            Results.Ok(await reports.GetShelterStayStatsAsync()))
            .RequireAuthorization();

        return api;
    }
}
