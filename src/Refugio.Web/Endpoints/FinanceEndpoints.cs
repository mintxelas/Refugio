using System.Text;
using Refugio.Application.Actors;
using Refugio.Application.Messages;
using Refugio.Application.Services;
using Refugio.Domain.Entities;

namespace Refugio.Web.Endpoints;

public static class FinanceEndpoints
{
    public static RouteGroupBuilder MapFinanceEndpoints(this RouteGroupBuilder api)
    {
        // Donations
        api.MapGet("/donations", async (ShelterActorService actors) =>
            Results.Ok(await actors.Ask<List<Donation>>(new GetAllDonations())));

        api.MapGet("/donations/{id:int}", async (int id, ShelterActorService actors) =>
        {
            var d = await actors.Ask<Donation?>(new GetDonationById(id));
            return d is null ? Results.NotFound() : Results.Ok(d);
        });

        api.MapPost("/donations", async (CreateDonation cmd, ShelterActorService actors) =>
        {
            var d = await actors.Ask<Donation>(cmd);
            return Results.Created($"/api/donations/{d.Id}", d);
        });

        api.MapPut("/donations/{id:int}", async (int id, UpdateDonation cmd, ShelterActorService actors) =>
        {
            var d = await actors.Ask<Donation?>(cmd with { Id = id });
            return d is null ? Results.NotFound() : Results.Ok(d);
        });

        api.MapDelete("/donations/{id:int}", async (int id, ShelterActorService actors) =>
        {
            var ok = await actors.Ask<bool>(new DeleteDonation(id));
            return ok ? Results.NoContent() : Results.NotFound();
        });

        api.MapPost("/donations/{id:int}/delete", async (int id, ShelterActorService actors) =>
        {
            await actors.Ask<bool>(new DeleteDonation(id));
            return Results.Redirect("/funds");
        }).RequireAuthorization("Manager");

        api.MapPost("/donations/{id:int}/restore", async (int id, ShelterActorService actors) =>
        {
            await actors.Ask<bool>(new RestoreDonation(id));
            return Results.Redirect("/admin/deleted?tab=donations");
        }).RequireAuthorization("Manager");

        // Expenses
        api.MapGet("/expenses", async (ShelterActorService actors) =>
            Results.Ok(await actors.Ask<List<Expense>>(new GetAllExpenses())));

        api.MapGet("/expenses/{id:int}", async (int id, ShelterActorService actors) =>
        {
            var e = await actors.Ask<Expense?>(new GetExpenseById(id));
            return e is null ? Results.NotFound() : Results.Ok(e);
        });

        api.MapPost("/expenses", async (CreateExpense cmd, ShelterActorService actors) =>
        {
            var e = await actors.Ask<Expense>(cmd);
            return Results.Created($"/api/expenses/{e.Id}", e);
        });

        api.MapPut("/expenses/{id:int}", async (int id, UpdateExpense cmd, ShelterActorService actors) =>
        {
            var e = await actors.Ask<Expense?>(cmd with { Id = id });
            return e is null ? Results.NotFound() : Results.Ok(e);
        });

        api.MapDelete("/expenses/{id:int}", async (int id, ShelterActorService actors) =>
        {
            var ok = await actors.Ask<bool>(new DeleteExpense(id));
            return ok ? Results.NoContent() : Results.NotFound();
        });

        api.MapPost("/expenses/{id:int}/delete", async (int id, ShelterActorService actors) =>
        {
            await actors.Ask<bool>(new DeleteExpense(id));
            return Results.Redirect("/funds");
        }).RequireAuthorization("Manager");

        api.MapPost("/expenses/{id:int}/restore", async (int id, ShelterActorService actors) =>
        {
            await actors.Ask<bool>(new RestoreExpense(id));
            return Results.Redirect("/admin/deleted?tab=expenses");
        }).RequireAuthorization("Manager");

        // Finance summary
        api.MapGet("/finances/summary", async (int? year, ShelterActorService actors) =>
            Results.Ok(await actors.Ask<FinanceSummary>(new GetFinanceSummary(year ?? DateTime.UtcNow.Year))));

        // CSV exports
        api.MapGet("/export/donations", async (ShelterActorService actors) =>
        {
            var donations = await actors.Ask<List<Donation>>(new GetAllDonations());
            var rows = new List<string> { "Date,Donor Name,Category,Amount,Notes" };
            rows.AddRange(donations.Select(d => string.Join(",",
                CsvField(d.Date.ToString("yyyy-MM-dd")),
                CsvField(d.DonorName),
                CsvField(d.Category.ToString()),
                CsvField(d.Amount.ToString("F2")),
                CsvField(d.Notes))));
            return Results.File(ToCsvBytes(rows), "text/csv", $"donations-{DateTime.UtcNow:yyyy-MM-dd}.csv");
        }).RequireAuthorization();

        api.MapGet("/export/expenses", async (ShelterActorService actors) =>
        {
            var expenses = await actors.Ask<List<Expense>>(new GetAllExpenses());
            var rows = new List<string> { "Date,Description,Category,Amount,Notes" };
            rows.AddRange(expenses.Select(e => string.Join(",",
                CsvField(e.Date.ToString("yyyy-MM-dd")),
                CsvField(e.Description),
                CsvField(e.Category),
                CsvField(e.Amount.ToString("F2")),
                CsvField(e.Notes))));
            return Results.File(ToCsvBytes(rows), "text/csv", $"expenses-{DateTime.UtcNow:yyyy-MM-dd}.csv");
        }).RequireAuthorization();

        api.MapGet("/export/adoptions", async (ShelterActorService actors) =>
        {
            var adoptions = await actors.Ask<List<Adoption>>(new GetAllAdoptions());
            var rows = new List<string> { "ID,Applicant Name,Email,Phone,Type,Status,Dog Name,Created,Updated,Notes" };
            rows.AddRange(adoptions.Select(a => string.Join(",",
                CsvField(a.Id),
                CsvField(a.ApplicantName),
                CsvField(a.ApplicantEmail),
                CsvField(a.ApplicantPhone),
                CsvField(a.Type.ToString()),
                CsvField(a.Status.ToString()),
                CsvField(a.Dog?.Name),
                CsvField(a.CreatedAt.ToString("yyyy-MM-dd")),
                CsvField(a.UpdatedAt?.ToString("yyyy-MM-dd")),
                CsvField(a.Notes))));
            return Results.File(ToCsvBytes(rows), "text/csv", $"adoptions-{DateTime.UtcNow:yyyy-MM-dd}.csv");
        }).RequireAuthorization();

        return api;
    }

    private static string CsvField(object? value)
    {
        var s = value?.ToString() ?? "";
        return s.Contains(',') || s.Contains('"') || s.Contains('\n')
            ? $"\"{s.Replace("\"", "\"\"")}\""
            : s;
    }

    private static byte[] ToCsvBytes(IEnumerable<string> rows)
        => Encoding.UTF8.GetBytes(string.Join("\r\n", rows));
}
