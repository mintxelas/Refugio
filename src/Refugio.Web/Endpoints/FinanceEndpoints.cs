using System.Text;
using Refugio.Application.Contracts;
using Refugio.Application.Queries;
using Refugio.Application.Services;
using Refugio.Web.Helpers;

namespace Refugio.Web.Endpoints;

public static class FinanceEndpoints
{
    public static RouteGroupBuilder MapFinanceEndpoints(this RouteGroupBuilder api)
    {
        // Donations
        api.MapGet("/donations", async (IFinanceService finance) =>
            Results.Ok(await finance.GetDonationsAsync()));

        api.MapGet("/donations/paged", async (int? page, int? pageSize, IFinanceService finance) =>
            Results.Ok(await finance.GetDonationsPagedAsync(page ?? 1, pageSize ?? 25)));

        api.MapGet("/donations/deleted", async (IFinanceService finance) =>
            Results.Ok(await finance.GetDeletedDonationsAsync())).RequireAuthorization("Manager");

        api.MapGet("/donations/deleted/{id:int}", async (int id, IFinanceService finance) =>
        {
            var donation = await finance.GetDeletedDonationAsync(id);
            return donation is null ? Results.NotFound() : Results.Ok(donation);
        }).RequireAuthorization("Manager");

        api.MapGet("/donations/{id:int}", async (int id, IFinanceService finance) =>
        {
            var donation = await finance.GetDonationAsync(id);
            return donation is null ? Results.NotFound() : Results.Ok(donation);
        });

        api.MapPost("/donations", async (CreateDonationRequest request, IFinanceService finance) =>
        {
            var donation = await finance.RecordDonationAsync(request);
            return Results.Created($"/api/donations/{donation.Id}", donation);
        });

        api.MapPut("/donations/{id:int}", async (int id, UpdateDonationRequest request, IFinanceService finance) =>
        {
            var donation = await finance.UpdateDonationAsync(request with { Id = id });
            return donation is null ? Results.NotFound() : Results.Ok(donation);
        });

        api.MapDelete("/donations/{id:int}", async (int id, IFinanceService finance) =>
            await finance.DeleteDonationAsync(id) ? Results.NoContent() : Results.NotFound());

        api.MapPost("/donations/{id:int}/delete", async (int id, IFinanceService finance) =>
        {
            await finance.DeleteDonationAsync(id);
            return Results.Redirect("/funds");
        }).RequireAuthorization("Manager");

        api.MapPost("/donations/{id:int}/restore", async (int id, IFinanceService finance) =>
        {
            await finance.RestoreDonationAsync(id);
            return Results.Redirect("/admin/deleted?tab=donations");
        }).RequireAuthorization("Manager");

        api.MapPost("/donations/{id:int}/purge", async (int id, IFinanceService finance) =>
        {
            await finance.PurgeDonationAsync(id);
            return Results.Redirect("/admin/deleted?tab=donations");
        }).RequireAuthorization("Manager");

        // Expenses
        api.MapGet("/expenses", async (IFinanceService finance) =>
            Results.Ok(await finance.GetExpensesAsync()));

        api.MapGet("/expenses/paged", async (int? page, int? pageSize, IFinanceService finance) =>
            Results.Ok(await finance.GetExpensesPagedAsync(page ?? 1, pageSize ?? 25)));

        api.MapGet("/expenses/deleted", async (IFinanceService finance) =>
            Results.Ok(await finance.GetDeletedExpensesAsync())).RequireAuthorization("Manager");

        api.MapGet("/expenses/deleted/{id:int}", async (int id, IFinanceService finance) =>
        {
            var expense = await finance.GetDeletedExpenseAsync(id);
            return expense is null ? Results.NotFound() : Results.Ok(expense);
        }).RequireAuthorization("Manager");

        api.MapGet("/expenses/{id:int}", async (int id, IFinanceService finance) =>
        {
            var expense = await finance.GetExpenseAsync(id);
            return expense is null ? Results.NotFound() : Results.Ok(expense);
        });

        api.MapPost("/expenses", async (CreateExpenseRequest request, IFinanceService finance) =>
        {
            var expense = await finance.RecordExpenseAsync(request);
            return Results.Created($"/api/expenses/{expense.Id}", expense);
        });

        api.MapPut("/expenses/{id:int}", async (int id, UpdateExpenseRequest request, IFinanceService finance) =>
        {
            var expense = await finance.UpdateExpenseAsync(request with { Id = id });
            return expense is null ? Results.NotFound() : Results.Ok(expense);
        });

        api.MapDelete("/expenses/{id:int}", async (int id, IFinanceService finance) =>
            await finance.DeleteExpenseAsync(id) ? Results.NoContent() : Results.NotFound());

        api.MapPost("/expenses/{id:int}/delete", async (int id, IFinanceService finance) =>
        {
            await finance.DeleteExpenseAsync(id);
            return Results.Redirect("/funds");
        }).RequireAuthorization("Manager");

        api.MapPost("/expenses/{id:int}/restore", async (int id, IFinanceService finance) =>
        {
            await finance.RestoreExpenseAsync(id);
            return Results.Redirect("/admin/deleted?tab=expenses");
        }).RequireAuthorization("Manager");

        api.MapPost("/expenses/{id:int}/purge", async (int id, IFinanceService finance) =>
        {
            await finance.PurgeExpenseAsync(id);
            return Results.Redirect("/admin/deleted?tab=expenses");
        }).RequireAuthorization("Manager");

        // Expense receipt gallery — multiple images per expense, no default
        api.MapGet("/expenses/{id:int}/photos", async (int id, IFinanceService finance) =>
            Results.Ok(await finance.GetExpensePhotosAsync(id)));

        api.MapPost("/expenses/{id:int}/photos", async (int id, HttpContext ctx, IFinanceService finance, IWebHostEnvironment env) =>
        {
            var dir = Path.Combine(env.WebRootPath, "expenses");
            Directory.CreateDirectory(dir);
            foreach (var file in ctx.Request.Form.Files.GetFiles("Photos"))
            {
                if (file.Length == 0 || file.Length > 5 * 1024 * 1024) continue;
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp")) continue;
                var fileName = $"{id}_{Guid.NewGuid():N}{ext}";
                await using var stream = File.Create(Path.Combine(dir, fileName));
                await file.CopyToAsync(stream);
                await finance.AddExpensePhotoAsync(id, $"/expenses/{fileName}");
            }
            return Results.Redirect($"/funds/expenses/{id}");
        }).RequireAuthorization().DisableAntiforgery();

        // JSON upload variant for the React SPA.
        api.MapPost("/expenses/{id:int}/photos/upload", async (int id, HttpContext ctx, IFinanceService finance, IWebHostEnvironment env) =>
        {
            var dir = Path.Combine(env.WebRootPath, "expenses");
            Directory.CreateDirectory(dir);
            var uploaded = new List<string>();
            foreach (var file in ctx.Request.Form.Files.GetFiles("Photos"))
            {
                if (file.Length == 0 || file.Length > 5 * 1024 * 1024) continue;
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp")) continue;
                var fileName = $"{id}_{Guid.NewGuid():N}{ext}";
                await using var stream = File.Create(Path.Combine(dir, fileName));
                await file.CopyToAsync(stream);
                var url = $"/expenses/{fileName}";
                await finance.AddExpensePhotoAsync(id, url);
                uploaded.Add(url);
            }
            return uploaded.Count == 0 ? Results.BadRequest(new { error = "no_valid_files" }) : Results.Ok(new { urls = uploaded });
        }).RequireAuthorization().DisableAntiforgery();

        api.MapPost("/expenses/photos/{photoId:int}/delete", async (int photoId, int expenseId, IFinanceService finance, IWebHostEnvironment env) =>
        {
            var url = await finance.RemoveExpensePhotoAsync(photoId);
            PhotoFiles.DeleteByUrl(env, url);
            return Results.Redirect($"/funds/expenses/{expenseId}");
        }).RequireAuthorization();

        // Goals
        api.MapGet("/goals", async (IFinanceService finance) =>
            Results.Ok(await finance.GetGoalsAsync()));

        api.MapGet("/goals/deleted", async (IFinanceService finance) =>
            Results.Ok(await finance.GetDeletedGoalsAsync())).RequireAuthorization("Manager");

        api.MapGet("/goals/deleted/{id:int}", async (int id, IFinanceService finance) =>
        {
            var goal = await finance.GetDeletedGoalAsync(id);
            return goal is null ? Results.NotFound() : Results.Ok(goal);
        }).RequireAuthorization("Manager");

        api.MapGet("/goals/{id:int}", async (int id, IFinanceService finance) =>
        {
            var goal = await finance.GetGoalAsync(id);
            return goal is null ? Results.NotFound() : Results.Ok(goal);
        });

        api.MapPost("/goals", async (CreateGoalRequest request, IFinanceService finance) =>
        {
            var goal = await finance.CreateGoalAsync(request);
            return Results.Created($"/api/goals/{goal.Id}", goal);
        });

        api.MapPut("/goals/{id:int}", async (int id, UpdateGoalRequest request, IFinanceService finance) =>
        {
            var goal = await finance.UpdateGoalAsync(request with { Id = id });
            return goal is null ? Results.NotFound() : Results.Ok(goal);
        });

        api.MapDelete("/goals/{id:int}", async (int id, IFinanceService finance) =>
            await finance.DeleteGoalAsync(id) ? Results.NoContent() : Results.NotFound());

        api.MapPost("/goals/{id:int}/delete", async (int id, IFinanceService finance) =>
        {
            await finance.DeleteGoalAsync(id);
            return Results.Redirect("/funds?tab=goals");
        }).RequireAuthorization("Manager");

        api.MapPost("/goals/{id:int}/restore", async (int id, IFinanceService finance) =>
        {
            await finance.RestoreGoalAsync(id);
            return Results.Redirect("/admin/deleted?tab=goals");
        }).RequireAuthorization("Manager");

        api.MapPost("/goals/{id:int}/purge", async (int id, IFinanceService finance) =>
        {
            await finance.PurgeGoalAsync(id);
            return Results.Redirect("/admin/deleted?tab=goals");
        }).RequireAuthorization("Manager");

        // Finance summary
        api.MapGet("/finances/summary", async (int? year, IFinanceQueries financeQueries) =>
            Results.Ok(await financeQueries.GetSummaryAsync(year ?? DateTime.UtcNow.Year)));

        // CSV exports
        api.MapGet("/export/donations", async (IFinanceService finance) =>
        {
            var donations = await finance.GetDonationsAsync();
            var rows = new List<string> { "Date,Donor Name,Category,Amount,Notes" };
            rows.AddRange(donations.Select(d => string.Join(",",
                CsvField(d.Date.ToString("yyyy-MM-dd")),
                CsvField(d.DonorName),
                CsvField(d.Category.ToString()),
                CsvField(d.Amount.ToString("F2")),
                CsvField(d.Notes))));
            return Results.File(ToCsvBytes(rows), "text/csv", $"donations-{DateTime.UtcNow:yyyy-MM-dd}.csv");
        }).RequireAuthorization();

        api.MapGet("/export/expenses", async (IFinanceService finance) =>
        {
            var expenses = await finance.GetExpensesAsync();
            var rows = new List<string> { "Date,Description,Category,Amount,Notes" };
            rows.AddRange(expenses.Select(e => string.Join(",",
                CsvField(e.Date.ToString("yyyy-MM-dd")),
                CsvField(e.Description),
                CsvField(e.Category),
                CsvField(e.Amount.ToString("F2")),
                CsvField(e.Notes))));
            return Results.File(ToCsvBytes(rows), "text/csv", $"expenses-{DateTime.UtcNow:yyyy-MM-dd}.csv");
        }).RequireAuthorization();

        api.MapGet("/export/adoptions", async (IAdoptionService adoptions) =>
        {
            var all = await adoptions.GetAdoptionsAsync();
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
        return s.Contains(',') || s.Contains('"') || s.Contains('\n')
            ? $"\"{s.Replace("\"", "\"\"")}\""
            : s;
    }

    private static byte[] ToCsvBytes(IEnumerable<string> rows)
        => Encoding.UTF8.GetBytes(string.Join("\r\n", rows));
}
