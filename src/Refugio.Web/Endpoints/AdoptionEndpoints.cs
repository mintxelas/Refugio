using Akka.Hosting;
using Refugio.Actors;
using Refugio.Actors.Messages;
using Refugio.Application.Contracts;
using Refugio.Application.Queries;
using Refugio.Domain.Common;
using Refugio.Domain.Entities;

namespace Refugio.Web.Endpoints;

public static class AdoptionEndpoints
{
    public static RouteGroupBuilder MapAdoptionEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/adoptions", async (AdoptionStatus? status, IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<AdoptionActor>().AskRequired<List<AdoptionDto>>(new GetAdoptions(status), ct)));

        api.MapGet("/adoptions/paged", async (AdoptionStatus? status, int? page, int? pageSize, IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<AdoptionActor>().AskRequired<Page<AdoptionDto>>(new GetAdoptionsPaged(status, page ?? 1, pageSize ?? 25), ct)));

        api.MapGet("/adoptions/deleted", async (IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<AdoptionActor>().AskRequired<List<AdoptionDto>>(new GetDeletedAdoptions(), ct)))
            .RequireAuthorization("Manager");

        api.MapGet("/adoptions/deleted/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            var adoption = await actors.Get<AdoptionActor>().AskFor<AdoptionDto>(new GetDeletedAdoption(id), ct);
            return adoption is null ? Results.NotFound() : Results.Ok(adoption);
        }).RequireAuthorization("Manager");

        api.MapGet("/adoptions/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            var adoption = await actors.Get<AdoptionActor>().AskFor<AdoptionDto>(new GetAdoption(id), ct);
            return adoption is null ? Results.NotFound() : Results.Ok(adoption);
        });

        api.MapPost("/adoptions", async (CreateAdoptionRequest request, IActorRegistry actors, CancellationToken ct) =>
        {
            var adoption = await actors.Get<AdoptionActor>().AskRequired<AdoptionDto>(request, ct);
            return Results.Created($"/api/adoptions/{adoption.Id}", adoption);
        });

        api.MapPut("/adoptions/{id:int}", async (int id, UpdateAdoptionRequest request, IActorRegistry actors, CancellationToken ct) =>
        {
            var adoption = await actors.Get<AdoptionActor>().AskFor<AdoptionDto>(request with { Id = id }, ct);
            return adoption is null ? Results.NotFound() : Results.Ok(adoption);
        });

        api.MapPut("/adoptions/{id:int}/status", async (int id, UpdateAdoptionStatusRequest request, IActorRegistry actors, CancellationToken ct) =>
        {
            var adoption = await actors.Get<AdoptionActor>().AskFor<AdoptionDto>(request with { Id = id }, ct);
            return adoption is null ? Results.NotFound() : Results.Ok(adoption);
        });

        api.MapDelete("/adoptions/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
            await actors.Get<AdoptionActor>().AskRequired<bool>(new DeleteAdoption(id), ct)
                ? Results.NoContent() : Results.NotFound());

        api.MapPost("/adoptions/{id:int}/delete", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<AdoptionActor>().AskRequired<bool>(new DeleteAdoption(id), ct);
            return Results.Redirect("/adoptions");
        }).RequireAuthorization("Manager");

        api.MapPost("/adoptions/{id:int}/restore", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<AdoptionActor>().AskRequired<bool>(new RestoreAdoption(id), ct);
            return Results.Redirect("/admin/deleted?tab=adoptions");
        }).RequireAuthorization("Manager");

        api.MapPost("/adoptions/{id:int}/purge", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<AdoptionActor>().AskRequired<bool>(new PurgeAdoption(id), ct);
            return Results.Redirect("/admin/deleted?tab=adoptions");
        }).RequireAuthorization("Manager");

        api.MapPost("/adoptions/{id:int}/advance", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<AdoptionActor>().AskFor<AdoptionDto>(new AdvanceAdoption(id), ct);
            return Results.Redirect("/adoptions");
        }).RequireAuthorization();

        api.MapPost("/adoptions/{id:int}/reject", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<AdoptionActor>().AskFor<AdoptionDto>(
                new UpdateAdoptionStatusRequest(id, AdoptionStatus.Rejected, null), ct);
            return Results.Redirect("/adoptions");
        }).RequireAuthorization();

        // Reports (sourced from adoption data) — CQRS read models stay direct.
        api.MapGet("/reports/adoption-conversion", async (int? year, IAdoptionQueries reports) =>
            Results.Ok(await reports.GetConversionStatsAsync(year ?? DateTime.UtcNow.Year)))
            .RequireAuthorization();

        api.MapGet("/reports/shelter-stay", async (IAdoptionQueries reports) =>
            Results.Ok(await reports.GetShelterStayStatsAsync()))
            .RequireAuthorization();

        return api;
    }
}
