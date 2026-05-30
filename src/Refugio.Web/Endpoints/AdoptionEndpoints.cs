using Refugio.Application.Messages;
using Refugio.Application.Services;
using Refugio.Domain.Entities;

namespace Refugio.Web.Endpoints;

public static class AdoptionEndpoints
{
    public static RouteGroupBuilder MapAdoptionEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/adoptions", async (AdoptionStatus? status, ShelterActorService actors) =>
            Results.Ok(await actors.Ask<List<Adoption>>(new GetAllAdoptions(status))));

        api.MapGet("/adoptions/{id:int}", async (int id, ShelterActorService actors) =>
        {
            var a = await actors.Ask<Adoption?>(new GetAdoptionById(id));
            return a is null ? Results.NotFound() : Results.Ok(a);
        });

        api.MapPost("/adoptions", async (CreateAdoption cmd, ShelterActorService actors) =>
        {
            var a = await actors.Ask<Adoption>(cmd);
            return Results.Created($"/api/adoptions/{a.Id}", a);
        });

        api.MapPut("/adoptions/{id:int}/status", async (int id, UpdateAdoptionStatus cmd, ShelterActorService actors) =>
        {
            var a = await actors.Ask<Adoption?>(cmd with { Id = id });
            return a is null ? Results.NotFound() : Results.Ok(a);
        });

        api.MapDelete("/adoptions/{id:int}", async (int id, ShelterActorService actors) =>
        {
            var ok = await actors.Ask<bool>(new DeleteAdoption(id));
            return ok ? Results.NoContent() : Results.NotFound();
        });

        api.MapPost("/adoptions/{id:int}/delete", async (int id, ShelterActorService actors) =>
        {
            await actors.Ask<bool>(new DeleteAdoption(id));
            return Results.Redirect("/adoptions");
        }).RequireAuthorization("Manager");

        api.MapPost("/adoptions/{id:int}/restore", async (int id, ShelterActorService actors) =>
        {
            await actors.Ask<bool>(new RestoreAdoption(id));
            return Results.Redirect("/admin/deleted?tab=adoptions");
        }).RequireAuthorization("Manager");

        api.MapPost("/adoptions/{id:int}/advance", async (int id, ShelterActorService actors) =>
        {
            var adoption = await actors.Ask<Adoption?>(new GetAdoptionById(id));
            if (adoption is not null)
            {
                var next = adoption.Status switch
                {
                    AdoptionStatus.Applied   => AdoptionStatus.Interview,
                    AdoptionStatus.Interview => AdoptionStatus.HomeCheck,
                    AdoptionStatus.HomeCheck => AdoptionStatus.Approved,
                    AdoptionStatus.Approved  => AdoptionStatus.Finalized,
                    _                        => adoption.Status
                };
                await actors.Ask<Adoption?>(new UpdateAdoptionStatus(id, next, null));
            }
            return Results.Redirect("/adoptions");
        }).RequireAuthorization();

        api.MapPost("/adoptions/{id:int}/reject", async (int id, ShelterActorService actors) =>
        {
            await actors.Ask<Adoption?>(new UpdateAdoptionStatus(id, AdoptionStatus.Rejected, null));
            return Results.Redirect("/adoptions");
        }).RequireAuthorization();

        // Reports (sourced from adoption data)
        api.MapGet("/reports/adoption-conversion", async (int? year, ShelterActorService actors) =>
            Results.Ok(await actors.Ask<AdoptionConversionStats>(new GetAdoptionConversionStats(year ?? DateTime.UtcNow.Year))))
            .RequireAuthorization();

        api.MapGet("/reports/shelter-stay", async (ShelterActorService actors) =>
            Results.Ok(await actors.Ask<ShelterStayStats>(new GetShelterStayStats())))
            .RequireAuthorization();

        return api;
    }
}
