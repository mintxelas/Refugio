using System.Text;
using Akka.Hosting;
using Refugio.Actors;
using Refugio.Actors.Messages;
using Refugio.Application.Contracts;
using Refugio.Application.Queries;
using Refugio.Domain.Common;
using Refugio.Web.Helpers;

namespace Refugio.Web.Endpoints;

public static class FinanceEndpoints
{
    public static RouteGroupBuilder MapFinanceEndpoints(this RouteGroupBuilder api)
    {
        // Donations
        api.MapGet("/donations", async (IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<FinanceActor>().AskRequired<List<DonationDto>>(new GetDonations(), ct)));

        api.MapGet("/donations/paged", async (int? page, int? pageSize, IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<FinanceActor>().AskRequired<Page<DonationDto>>(new GetDonationsPaged(page ?? 1, pageSize ?? 25), ct)));

        api.MapGet("/donations/deleted", async (IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<FinanceActor>().AskRequired<List<DonationDto>>(new GetDeletedDonations(), ct)))
            .RequireAuthorization("Manager");

        api.MapGet("/donations/deleted/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            var donation = await actors.Get<FinanceActor>().AskFor<DonationDto>(new GetDeletedDonation(id), ct);
            return donation is null ? Results.NotFound() : Results.Ok(donation);
        }).RequireAuthorization("Manager");

        api.MapGet("/donations/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            var donation = await actors.Get<FinanceActor>().AskFor<DonationDto>(new GetDonation(id), ct);
            return donation is null ? Results.NotFound() : Results.Ok(donation);
        });

        api.MapPost("/donations", async (CreateDonationRequest request, IActorRegistry actors, CancellationToken ct) =>
        {
            var donation = await actors.Get<FinanceActor>().AskRequired<DonationDto>(request, ct);
            return Results.Created($"/api/donations/{donation.Id}", donation);
        });

        api.MapPut("/donations/{id:int}", async (int id, UpdateDonationRequest request, IActorRegistry actors, CancellationToken ct) =>
        {
            var donation = await actors.Get<FinanceActor>().AskFor<DonationDto>(request with { Id = id }, ct);
            return donation is null ? Results.NotFound() : Results.Ok(donation);
        });

        api.MapDelete("/donations/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
            await actors.Get<FinanceActor>().AskRequired<bool>(new DeleteDonation(id), ct)
                ? Results.NoContent() : Results.NotFound())
            .RequireAuthorization("Manager");

        api.MapPost("/donations/{id:int}/delete", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<FinanceActor>().AskRequired<bool>(new DeleteDonation(id), ct);
            return Results.Redirect("/funds");
        }).RequireAuthorization("Manager");

        api.MapPost("/donations/{id:int}/restore", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<FinanceActor>().AskRequired<bool>(new RestoreDonation(id), ct);
            return Results.Redirect("/admin/deleted?tab=donations");
        }).RequireAuthorization("Manager");

        api.MapPost("/donations/{id:int}/purge", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<FinanceActor>().AskRequired<bool>(new PurgeDonation(id), ct);
            return Results.Redirect("/admin/deleted?tab=donations");
        }).RequireAuthorization("Manager");

        // Expenses
        api.MapGet("/expenses", async (IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<FinanceActor>().AskRequired<List<ExpenseDto>>(new GetExpenses(), ct)));

        api.MapGet("/expenses/paged", async (int? page, int? pageSize, IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<FinanceActor>().AskRequired<Page<ExpenseDto>>(new GetExpensesPaged(page ?? 1, pageSize ?? 25), ct)));

        api.MapGet("/expenses/deleted", async (IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<FinanceActor>().AskRequired<List<ExpenseDto>>(new GetDeletedExpenses(), ct)))
            .RequireAuthorization("Manager");

        api.MapGet("/expenses/deleted/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            var expense = await actors.Get<FinanceActor>().AskFor<ExpenseDto>(new GetDeletedExpense(id), ct);
            return expense is null ? Results.NotFound() : Results.Ok(expense);
        }).RequireAuthorization("Manager");

        api.MapGet("/expenses/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            var expense = await actors.Get<FinanceActor>().AskFor<ExpenseDto>(new GetExpense(id), ct);
            return expense is null ? Results.NotFound() : Results.Ok(expense);
        });

        api.MapPost("/expenses", async (CreateExpenseRequest request, IActorRegistry actors, CancellationToken ct) =>
        {
            var expense = await actors.Get<FinanceActor>().AskRequired<ExpenseDto>(request, ct);
            return Results.Created($"/api/expenses/{expense.Id}", expense);
        });

        api.MapPut("/expenses/{id:int}", async (int id, UpdateExpenseRequest request, IActorRegistry actors, CancellationToken ct) =>
        {
            var expense = await actors.Get<FinanceActor>().AskFor<ExpenseDto>(request with { Id = id }, ct);
            return expense is null ? Results.NotFound() : Results.Ok(expense);
        });

        api.MapDelete("/expenses/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
            await actors.Get<FinanceActor>().AskRequired<bool>(new DeleteExpense(id), ct)
                ? Results.NoContent() : Results.NotFound())
            .RequireAuthorization("Manager");

        api.MapPost("/expenses/{id:int}/delete", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<FinanceActor>().AskRequired<bool>(new DeleteExpense(id), ct);
            return Results.Redirect("/funds");
        }).RequireAuthorization("Manager");

        api.MapPost("/expenses/{id:int}/restore", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<FinanceActor>().AskRequired<bool>(new RestoreExpense(id), ct);
            return Results.Redirect("/admin/deleted?tab=expenses");
        }).RequireAuthorization("Manager");

        api.MapPost("/expenses/{id:int}/purge", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<FinanceActor>().AskRequired<bool>(new PurgeExpense(id), ct);
            return Results.Redirect("/admin/deleted?tab=expenses");
        }).RequireAuthorization("Manager");

        // Expense receipt gallery — multiple images per expense, no default
        api.MapGet("/expenses/{id:int}/photos", async (int id, IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<FinanceActor>().AskRequired<List<ExpensePhotoDto>>(new GetExpensePhotos(id), ct)));

        api.MapPost("/expenses/{id:int}/photos", async (int id, HttpContext ctx, IActorRegistry actors, IWebHostEnvironment env, CancellationToken ct) =>
        {
            var dir = Path.Combine(env.WebRootPath, "photos", "finance", "expenses", id.ToString());
            Directory.CreateDirectory(dir);
            foreach (var file in ctx.Request.Form.Files.GetFiles("Photos"))
            {
                if (file.Length == 0 || file.Length > 2 * 1024 * 1024) continue;
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!PhotoFiles.IsImageExtension(ext)) continue;
                if (!PhotoFiles.HasImageBytes(file)) continue;
                var fileName = $"{Guid.NewGuid():N}{ext}";
                await using var stream = File.Create(Path.Combine(dir, fileName));
                await file.CopyToAsync(stream);
                await actors.Get<FinanceActor>().AskFor<ExpensePhotoDto>(new AddExpensePhoto(id, $"/photos/finance/expenses/{id}/{fileName}"), ct);
            }
            return Results.Redirect($"/funds/expenses/{id}");
        }).RequireAuthorization();

        // JSON upload variant for the React SPA.
        api.MapPost("/expenses/{id:int}/photos/upload", async (int id, HttpContext ctx, IActorRegistry actors, IWebHostEnvironment env, CancellationToken ct) =>
        {
            var dir = Path.Combine(env.WebRootPath, "photos", "finance", "expenses", id.ToString());
            Directory.CreateDirectory(dir);
            var uploaded = new List<string>();
            foreach (var file in ctx.Request.Form.Files.GetFiles("Photos"))
            {
                if (file.Length == 0 || file.Length > 2 * 1024 * 1024) continue;
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!PhotoFiles.IsImageExtension(ext)) continue;
                if (!PhotoFiles.HasImageBytes(file)) continue;
                var fileName = $"{Guid.NewGuid():N}{ext}";
                await using var stream = File.Create(Path.Combine(dir, fileName));
                await file.CopyToAsync(stream);
                var url = $"/photos/finance/expenses/{id}/{fileName}";
                await actors.Get<FinanceActor>().AskFor<ExpensePhotoDto>(new AddExpensePhoto(id, url), ct);
                uploaded.Add(url);
            }
            return uploaded.Count == 0 ? Results.BadRequest(new { error = "no_valid_files" }) : Results.Ok(new { urls = uploaded });
        }).RequireAuthorization();

        api.MapPost("/expenses/photos/{photoId:int}/delete", async (int photoId, IActorRegistry actors, IWebHostEnvironment env, CancellationToken ct) =>
        {
            var url = await actors.Get<FinanceActor>().AskFor<string>(new RemoveExpensePhoto(photoId), ct);
            PhotoFiles.DeleteByUrl(env, url);
            return Results.NoContent();
        }).RequireAuthorization();

        // Goals
        api.MapGet("/goals", async (IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<FinanceActor>().AskRequired<List<GoalDto>>(new GetGoals(), ct)));

        api.MapGet("/goals/deleted", async (IActorRegistry actors, CancellationToken ct) =>
            Results.Ok(await actors.Get<FinanceActor>().AskRequired<List<GoalDto>>(new GetDeletedGoals(), ct)))
            .RequireAuthorization("Manager");

        api.MapGet("/goals/deleted/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            var goal = await actors.Get<FinanceActor>().AskFor<GoalDto>(new GetDeletedGoal(id), ct);
            return goal is null ? Results.NotFound() : Results.Ok(goal);
        }).RequireAuthorization("Manager");

        api.MapGet("/goals/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            var goal = await actors.Get<FinanceActor>().AskFor<GoalDto>(new GetGoal(id), ct);
            return goal is null ? Results.NotFound() : Results.Ok(goal);
        });

        api.MapPost("/goals", async (CreateGoalRequest request, IActorRegistry actors, CancellationToken ct) =>
        {
            var goal = await actors.Get<FinanceActor>().AskRequired<GoalDto>(request, ct);
            return Results.Created($"/api/goals/{goal.Id}", goal);
        });

        api.MapPut("/goals/{id:int}", async (int id, UpdateGoalRequest request, IActorRegistry actors, CancellationToken ct) =>
        {
            var goal = await actors.Get<FinanceActor>().AskFor<GoalDto>(request with { Id = id }, ct);
            return goal is null ? Results.NotFound() : Results.Ok(goal);
        });

        api.MapDelete("/goals/{id:int}", async (int id, IActorRegistry actors, CancellationToken ct) =>
            await actors.Get<FinanceActor>().AskRequired<bool>(new DeleteGoal(id), ct)
                ? Results.NoContent() : Results.NotFound())
            .RequireAuthorization("Manager");

        api.MapPost("/goals/{id:int}/delete", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<FinanceActor>().AskRequired<bool>(new DeleteGoal(id), ct);
            return Results.Redirect("/funds?tab=goals");
        }).RequireAuthorization("Manager");

        api.MapPost("/goals/{id:int}/restore", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<FinanceActor>().AskRequired<bool>(new RestoreGoal(id), ct);
            return Results.Redirect("/admin/deleted?tab=goals");
        }).RequireAuthorization("Manager");

        api.MapPost("/goals/{id:int}/purge", async (int id, IActorRegistry actors, CancellationToken ct) =>
        {
            await actors.Get<FinanceActor>().AskRequired<bool>(new PurgeGoal(id), ct);
            return Results.Redirect("/admin/deleted?tab=goals");
        }).RequireAuthorization("Manager");

        // Finance summary — CQRS read model stays direct.
        api.MapGet("/finances/summary", async (int? year, IFinanceQueries financeQueries) =>
            Results.Ok(await financeQueries.GetSummaryAsync(year ?? DateTime.UtcNow.Year)));

        // CSV exports
        api.MapGet("/export/donations", async (IActorRegistry actors, CancellationToken ct) =>
        {
            var donations = await actors.Get<FinanceActor>().AskRequired<List<DonationDto>>(new GetDonations(), ct);
            var rows = new List<string> { "Date,Donor Name,Category,Amount,Notes,Tax ID" };
            rows.AddRange(donations.Select(d => string.Join(",",
                CsvField(d.Date.ToString("yyyy-MM-dd")),
                CsvField(d.DonorName),
                CsvField(d.Category.ToString()),
                CsvField(d.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                CsvField(d.Notes),
                CsvField(d.TaxId))));
            return Results.File(ToCsvBytes(rows), "text/csv", $"donations-{DateTime.UtcNow:yyyy-MM-dd}.csv");
        }).RequireAuthorization();

        api.MapGet("/export/expenses", async (IActorRegistry actors, CancellationToken ct) =>
        {
            var expenses = await actors.Get<FinanceActor>().AskRequired<List<ExpenseDto>>(new GetExpenses(), ct);
            var rows = new List<string> { "Date,Description,Category,Amount,Notes" };
            rows.AddRange(expenses.Select(e => string.Join(",",
                CsvField(e.Date.ToString("yyyy-MM-dd")),
                CsvField(e.Description),
                CsvField(e.Category),
                CsvField(e.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                CsvField(e.Notes))));
            return Results.File(ToCsvBytes(rows), "text/csv", $"expenses-{DateTime.UtcNow:yyyy-MM-dd}.csv");
        }).RequireAuthorization();

        api.MapGet("/export/adoptions", async (IActorRegistry actors, CancellationToken ct) =>
        {
            var all = await actors.Get<AdoptionActor>().AskRequired<List<AdoptionDto>>(new GetAdoptions(null), ct);
            var rows = new List<string> { "ID,Applicant Name,Email,Phone,Type,Status,Dog Name,Created,Updated,Notes" };
            rows.AddRange(all.Select(a => string.Join(",",
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
        // Prefix formula-injection chars so spreadsheets don't execute them.
        if (s.Length > 0 && s[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
            s = "'" + s;
        return s.Contains(',') || s.Contains('"') || s.Contains('\n')
            ? $"\"{s.Replace("\"", "\"\"")}\""
            : s;
    }

    private static byte[] ToCsvBytes(IEnumerable<string> rows)
        => Encoding.UTF8.GetBytes(string.Join("\r\n", rows));
}
