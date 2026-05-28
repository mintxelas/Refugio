using Akka.Actor;
using Akka.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using Refugio.Application.Actors;
using Refugio.Application.Messages;
using Refugio.Application.Services;
using Refugio.Domain.Entities;
using Refugio.Domain.Helpers;
using Refugio.Infrastructure.Data;
using Refugio.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ShelterDbContext>(opt =>
    opt.UseSqlite("Data Source=shelter.db"));

builder.Services.AddLocalization(opt => opt.ResourcesPath = "Resources");

var supportedCultures = new[] { new CultureInfo("en-US"), new CultureInfo("es-ES") };
builder.Services.Configure<RequestLocalizationOptions>(opts =>
{
    opts.DefaultRequestCulture = new RequestCulture("en-US");
    opts.SupportedCultures = supportedCultures;
    opts.SupportedUICultures = supportedCultures;
    opts.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
});

builder.Services.AddRazorComponents();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.LoginPath = "/login";
        opt.AccessDeniedPath = "/login";
        opt.ExpireTimeSpan = TimeSpan.FromDays(7);
        opt.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton(sp =>
{
    var setup = DependencyResolverSetup.Create(sp);
    var config = BootstrapSetup.Create();
    return ActorSystem.Create("ShelterSystem", config.And(setup));
});
builder.Services.AddSingleton<ShelterActorService>();
builder.Services.AddScoped<Refugio.Web.Services.ShelterApiClient>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();
    db.Database.Migrate();
    SeedData.Seed(db);
    var elena = db.Volunteers.FirstOrDefault(v => v.Email == "elena@havensanctuary.org");
    if (elena != null && !elena.CanLogin)
    {
        elena.CanLogin = true;
        elena.PasswordHash = PasswordHelper.Hash("shelter123");
        db.SaveChanges();
    }
}

_ = app.Services.GetRequiredService<ShelterActorService>();

if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Language switcher
app.MapGet("/set-language", (string? culture, string? returnUrl, HttpContext ctx) =>
{
    if (culture is "en-US" or "es-ES")
    {
        ctx.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });
    }
    return Results.Redirect(returnUrl ?? "/");
});

// Auth endpoints
app.MapPost("/auth/login", async (HttpContext ctx, ShelterActorService actors) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var email = form["email"].ToString().Trim();
    var password = form["password"].ToString();
    var volunteer = await actors.Ask<Volunteer?>(actors.Volunteers, new LoginVolunteer(email, password));
    if (volunteer is null) return Results.Redirect("/login?error=1");
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, volunteer.Id.ToString()),
        new(ClaimTypes.Name, volunteer.Name),
        new(ClaimTypes.Email, volunteer.Email),
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
    var ok = await actors.Ask<bool>(actors.Volunteers, new ChangeVolunteerPassword(int.Parse(userIdClaim), current, newPw));
    return Results.Redirect(ok ? "/change-password?success=1" : "/change-password?error=1");
}).RequireAuthorization().DisableAntiforgery();

var api = app.MapGroup("/api");

// Dashboard
api.MapGet("/dashboard", async (ShelterActorService actors) =>
    Results.Ok(await actors.Ask<DashboardStats>(actors.Dogs, new GetDashboardStats())));

// Dogs
api.MapGet("/dogs", async (string? search, DogStatus? status, ShelterActorService actors) =>
    Results.Ok(await actors.Ask<List<Dog>>(actors.Dogs, new GetAllDogs(search, status))));

api.MapGet("/dogs/{id:int}", async (int id, ShelterActorService actors) =>
{
    var dog = await actors.Ask<Dog?>(actors.Dogs, new GetDogById(id));
    return dog is null ? Results.NotFound() : Results.Ok(dog);
});

api.MapPost("/dogs", async (CreateDog cmd, ShelterActorService actors) =>
{
    var dog = await actors.Ask<Dog>(actors.Dogs, cmd);
    return Results.Created($"/api/dogs/{dog.Id}", dog);
});

api.MapPut("/dogs/{id:int}", async (int id, UpdateDog cmd, ShelterActorService actors) =>
{
    var dog = await actors.Ask<Dog?>(actors.Dogs, cmd with { Id = id });
    return dog is null ? Results.NotFound() : Results.Ok(dog);
});

api.MapDelete("/dogs/{id:int}", async (int id, ShelterActorService actors) =>
{
    var ok = await actors.Ask<bool>(actors.Dogs, new DeleteDog(id));
    return ok ? Results.NoContent() : Results.NotFound();
});

api.MapGet("/dogs/{id:int}/delete", async (int id, ShelterActorService actors) =>
{
    await actors.Ask<bool>(actors.Dogs, new DeleteDog(id));
    return Results.Redirect("/dogs");
}).RequireAuthorization();

api.MapPost("/dogs/{id:int}/photo", async (int id, HttpContext ctx, ShelterActorService actors, IWebHostEnvironment env) =>
{
    var file = ctx.Request.Form.Files.GetFile("Photo");
    if (file is null || file.Length == 0) return Results.Redirect($"/dogs/{id}/edit");
    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp")) return Results.Redirect($"/dogs/{id}/edit");
    if (file.Length > 5 * 1024 * 1024) return Results.Redirect($"/dogs/{id}/edit");
    var dir = Path.Combine(env.WebRootPath, "dogs");
    Directory.CreateDirectory(dir);
    foreach (var old in Directory.GetFiles(dir, $"{id}.*")) File.Delete(old);
    var fileName = $"{id}{ext}";
    await using var stream = File.Create(Path.Combine(dir, fileName));
    await file.CopyToAsync(stream);
    await actors.Ask<bool>(actors.Dogs, new UpdateDogPhoto(id, $"/dogs/{fileName}"));
    return Results.Redirect($"/dogs/{id}/edit");
}).RequireAuthorization().DisableAntiforgery();

// Medical Records
api.MapGet("/dogs/{id:int}/medical", async (int id, ShelterActorService actors) =>
    Results.Ok(await actors.Ask<List<MedicalRecord>>(actors.Dogs, new GetMedicalRecords(id))));

api.MapPost("/dogs/{id:int}/medical", async (int id, CreateMedicalRecord cmd, ShelterActorService actors) =>
{
    var rec = await actors.Ask<MedicalRecord>(actors.Dogs, cmd with { DogId = id });
    return Results.Created($"/api/dogs/{id}/medical/{rec.Id}", rec);
});

// Medications
api.MapGet("/dogs/{id:int}/medications", async (int id, ShelterActorService actors) =>
    Results.Ok(await actors.Ask<List<Medication>>(actors.Dogs, new GetMedications(id))));

api.MapPost("/dogs/{id:int}/medications", async (int id, CreateMedication cmd, ShelterActorService actors) =>
{
    var med = await actors.Ask<Medication>(actors.Dogs, cmd with { DogId = id });
    return Results.Created($"/api/dogs/{id}/medications/{med.Id}", med);
});

api.MapGet("/dogs/{id:int}/medical/{recId:int}", async (int recId, ShelterActorService actors) =>
{
    var rec = await actors.Ask<MedicalRecord?>(actors.Dogs, new GetMedicalRecordById(recId));
    return rec is null ? Results.NotFound() : Results.Ok(rec);
});

api.MapGet("/medical/{id:int}/delete", async (int id, int? dogId, ShelterActorService actors) =>
{
    await actors.Ask<bool>(actors.Dogs, new DeleteMedicalRecord(id));
    return Results.Redirect(dogId.HasValue ? $"/dogs/{dogId}" : "/dogs");
}).RequireAuthorization();

api.MapGet("/medications/{id:int}", async (int id, ShelterActorService actors) =>
{
    var med = await actors.Ask<Medication?>(actors.Dogs, new GetMedicationById(id));
    return med is null ? Results.NotFound() : Results.Ok(med);
});

api.MapGet("/medications/{id:int}/delete", async (int id, int? dogId, ShelterActorService actors) =>
{
    await actors.Ask<bool>(actors.Dogs, new DeleteMedication(id));
    return Results.Redirect(dogId.HasValue ? $"/dogs/{dogId}" : "/dogs");
}).RequireAuthorization();

api.MapDelete("/medications/{id:int}", async (int id, ShelterActorService actors) =>
{
    var ok = await actors.Ask<bool>(actors.Dogs, new DeactivateMedication(id));
    return ok ? Results.NoContent() : Results.NotFound();
});

// Adoptions
api.MapGet("/adoptions", async (AdoptionStatus? status, ShelterActorService actors) =>
    Results.Ok(await actors.Ask<List<Adoption>>(actors.Adoptions, new GetAllAdoptions(status))));

api.MapGet("/adoptions/{id:int}", async (int id, ShelterActorService actors) =>
{
    var a = await actors.Ask<Adoption?>(actors.Adoptions, new GetAdoptionById(id));
    return a is null ? Results.NotFound() : Results.Ok(a);
});

api.MapPost("/adoptions", async (CreateAdoption cmd, ShelterActorService actors) =>
{
    var a = await actors.Ask<Adoption>(actors.Adoptions, cmd);
    return Results.Created($"/api/adoptions/{a.Id}", a);
});

api.MapPut("/adoptions/{id:int}/status", async (int id, UpdateAdoptionStatus cmd, ShelterActorService actors) =>
{
    var a = await actors.Ask<Adoption?>(actors.Adoptions, cmd with { Id = id });
    return a is null ? Results.NotFound() : Results.Ok(a);
});

api.MapDelete("/adoptions/{id:int}", async (int id, ShelterActorService actors) =>
{
    var ok = await actors.Ask<bool>(actors.Adoptions, new DeleteAdoption(id));
    return ok ? Results.NoContent() : Results.NotFound();
});

api.MapGet("/adoptions/{id:int}/delete", async (int id, ShelterActorService actors) =>
{
    await actors.Ask<bool>(actors.Adoptions, new DeleteAdoption(id));
    return Results.Redirect("/adoptions");
}).RequireAuthorization();

api.MapGet("/adoptions/{id:int}/advance", async (int id, ShelterActorService actors) =>
{
    var adoption = await actors.Ask<Adoption?>(actors.Adoptions, new GetAdoptionById(id));
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
        await actors.Ask<Adoption?>(actors.Adoptions, new UpdateAdoptionStatus(id, next, null));
    }
    return Results.Redirect("/adoptions");
}).RequireAuthorization();

api.MapGet("/adoptions/{id:int}/reject", async (int id, ShelterActorService actors) =>
{
    await actors.Ask<Adoption?>(actors.Adoptions, new UpdateAdoptionStatus(id, AdoptionStatus.Rejected, null));
    return Results.Redirect("/adoptions");
}).RequireAuthorization();

// Tasks
api.MapGet("/tasks", async (bool? includeCompleted, ShelterActorService actors) =>
    Results.Ok(await actors.Ask<List<ShelterTask>>(actors.Tasks, new GetAllTasks(includeCompleted))));

api.MapPost("/tasks", async (CreateTask cmd, ShelterActorService actors) =>
{
    var t = await actors.Ask<ShelterTask>(actors.Tasks, cmd);
    return Results.Created($"/api/tasks/{t.Id}", t);
});

api.MapPut("/tasks/{id:int}/complete", async (int id, ShelterActorService actors) =>
{
    var ok = await actors.Ask<bool>(actors.Tasks, new CompleteTask(id));
    return ok ? Results.NoContent() : Results.NotFound();
});

api.MapGet("/tasks/{id:int}/complete", async (int id, ShelterActorService actors) =>
{
    await actors.Ask<bool>(actors.Tasks, new CompleteTask(id));
    return Results.Redirect("/");
}).RequireAuthorization();

api.MapGet("/tasks/{id:int}/delete", async (int id, ShelterActorService actors) =>
{
    await actors.Ask<bool>(actors.Tasks, new DeleteTask(id));
    return Results.Redirect("/");
}).RequireAuthorization();

api.MapDelete("/tasks/{id:int}", async (int id, ShelterActorService actors) =>
{
    var ok = await actors.Ask<bool>(actors.Tasks, new DeleteTask(id));
    return ok ? Results.NoContent() : Results.NotFound();
});

// Donations
api.MapGet("/donations", async (ShelterActorService actors) =>
    Results.Ok(await actors.Ask<List<Donation>>(actors.Finance, new GetAllDonations())));

api.MapGet("/donations/{id:int}", async (int id, ShelterActorService actors) =>
{
    var d = await actors.Ask<Donation?>(actors.Finance, new GetDonationById(id));
    return d is null ? Results.NotFound() : Results.Ok(d);
});

api.MapPost("/donations", async (CreateDonation cmd, ShelterActorService actors) =>
{
    var d = await actors.Ask<Donation>(actors.Finance, cmd);
    return Results.Created($"/api/donations/{d.Id}", d);
});

api.MapPut("/donations/{id:int}", async (int id, UpdateDonation cmd, ShelterActorService actors) =>
{
    var d = await actors.Ask<Donation?>(actors.Finance, cmd with { Id = id });
    return d is null ? Results.NotFound() : Results.Ok(d);
});

api.MapDelete("/donations/{id:int}", async (int id, ShelterActorService actors) =>
{
    var ok = await actors.Ask<bool>(actors.Finance, new DeleteDonation(id));
    return ok ? Results.NoContent() : Results.NotFound();
});

api.MapGet("/donations/{id:int}/delete", async (int id, ShelterActorService actors) =>
{
    await actors.Ask<bool>(actors.Finance, new DeleteDonation(id));
    return Results.Redirect("/funds");
}).RequireAuthorization();

// Expenses
api.MapGet("/expenses", async (ShelterActorService actors) =>
    Results.Ok(await actors.Ask<List<Expense>>(actors.Finance, new GetAllExpenses())));

api.MapGet("/expenses/{id:int}", async (int id, ShelterActorService actors) =>
{
    var e = await actors.Ask<Expense?>(actors.Finance, new GetExpenseById(id));
    return e is null ? Results.NotFound() : Results.Ok(e);
});

api.MapPost("/expenses", async (CreateExpense cmd, ShelterActorService actors) =>
{
    var e = await actors.Ask<Expense>(actors.Finance, cmd);
    return Results.Created($"/api/expenses/{e.Id}", e);
});

api.MapPut("/expenses/{id:int}", async (int id, UpdateExpense cmd, ShelterActorService actors) =>
{
    var e = await actors.Ask<Expense?>(actors.Finance, cmd with { Id = id });
    return e is null ? Results.NotFound() : Results.Ok(e);
});

api.MapDelete("/expenses/{id:int}", async (int id, ShelterActorService actors) =>
{
    var ok = await actors.Ask<bool>(actors.Finance, new DeleteExpense(id));
    return ok ? Results.NoContent() : Results.NotFound();
});

api.MapGet("/expenses/{id:int}/delete", async (int id, ShelterActorService actors) =>
{
    await actors.Ask<bool>(actors.Finance, new DeleteExpense(id));
    return Results.Redirect("/funds");
}).RequireAuthorization();

// Finance summary
api.MapGet("/finances/summary", async (int? year, ShelterActorService actors) =>
    Results.Ok(await actors.Ask<FinanceSummary>(actors.Finance, new GetFinanceSummary(year ?? DateTime.UtcNow.Year))));

// CSV exports
static string CsvField(object? value)
{
    var s = value?.ToString() ?? "";
    return s.Contains(',') || s.Contains('"') || s.Contains('\n')
        ? $"\"{s.Replace("\"", "\"\"")}\""
        : s;
}
static byte[] ToCsvBytes(IEnumerable<string> rows)
    => System.Text.Encoding.UTF8.GetBytes(string.Join("\r\n", rows));

api.MapGet("/export/donations", async (ShelterActorService actors) =>
{
    var donations = await actors.Ask<List<Donation>>(actors.Finance, new GetAllDonations());
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
    var expenses = await actors.Ask<List<Expense>>(actors.Finance, new GetAllExpenses());
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
    var adoptions = await actors.Ask<List<Adoption>>(actors.Adoptions, new GetAllAdoptions());
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

// Volunteers
api.MapGet("/volunteers", async (VolunteerStatus? status, ShelterActorService actors) =>
    Results.Ok(await actors.Ask<List<Volunteer>>(actors.Volunteers, new GetAllVolunteers(status))));

api.MapGet("/volunteers/{id:int}", async (int id, ShelterActorService actors) =>
{
    var v = await actors.Ask<Volunteer?>(actors.Volunteers, new GetVolunteerById(id));
    return v is null ? Results.NotFound() : Results.Ok(v);
});

api.MapPut("/volunteers/{id:int}", async (int id, UpdateVolunteer cmd, ShelterActorService actors) =>
{
    var v = await actors.Ask<Volunteer?>(actors.Volunteers, cmd with { Id = id });
    return v is null ? Results.NotFound() : Results.Ok(v);
});

api.MapPost("/volunteers", async (CreateVolunteer cmd, ShelterActorService actors) =>
{
    var v = await actors.Ask<Volunteer>(actors.Volunteers, cmd);
    return Results.Created($"/api/volunteers/{v.Id}", v);
});

api.MapPut("/volunteers/{id:int}/status", async (int id, UpdateVolunteerStatus cmd, ShelterActorService actors) =>
{
    var v = await actors.Ask<Volunteer?>(actors.Volunteers, cmd with { Id = id });
    return v is null ? Results.NotFound() : Results.Ok(v);
});

api.MapGet("/volunteers/{id:int}/activate", async (int id, ShelterActorService actors) =>
{
    await actors.Ask<Volunteer?>(actors.Volunteers, new UpdateVolunteerStatus(id, VolunteerStatus.Active));
    return Results.Redirect("/volunteers");
}).RequireAuthorization();

api.MapGet("/volunteers/{id:int}/deactivate", async (int id, ShelterActorService actors) =>
{
    await actors.Ask<Volunteer?>(actors.Volunteers, new UpdateVolunteerStatus(id, VolunteerStatus.Inactive));
    return Results.Redirect("/volunteers");
}).RequireAuthorization();

api.MapDelete("/volunteers/{id:int}", async (int id, ShelterActorService actors) =>
{
    var ok = await actors.Ask<bool>(actors.Volunteers, new DeleteVolunteer(id));
    return ok ? Results.NoContent() : Results.NotFound();
});

api.MapGet("/volunteers/{id:int}/delete", async (int id, ShelterActorService actors) =>
{
    await actors.Ask<bool>(actors.Volunteers, new DeleteVolunteer(id));
    return Results.Redirect("/volunteers");
}).RequireAuthorization();

// Events / Calendar
api.MapGet("/events", async (DateTime? from, DateTime? to, ShelterActorService actors) =>
    Results.Ok(await actors.Ask<List<ShelterEvent>>(actors.Volunteers, new GetAllEvents(from, to))));

api.MapGet("/events/{id:int}", async (int id, ShelterActorService actors) =>
{
    var e = await actors.Ask<ShelterEvent?>(actors.Volunteers, new GetEventById(id));
    return e is null ? Results.NotFound() : Results.Ok(e);
});

api.MapPost("/events", async (CreateEvent cmd, ShelterActorService actors) =>
{
    var e = await actors.Ask<ShelterEvent>(actors.Volunteers, cmd);
    return Results.Created($"/api/events/{e.Id}", e);
});

api.MapPut("/events/{id:int}", async (int id, UpdateEvent cmd, ShelterActorService actors) =>
{
    var e = await actors.Ask<ShelterEvent?>(actors.Volunteers, cmd with { Id = id });
    return e is null ? Results.NotFound() : Results.Ok(e);
});

api.MapDelete("/events/{id:int}", async (int id, ShelterActorService actors) =>
{
    var ok = await actors.Ask<bool>(actors.Volunteers, new DeleteEvent(id));
    return ok ? Results.NoContent() : Results.NotFound();
});

api.MapGet("/events/{id:int}/delete", async (int id, ShelterActorService actors) =>
{
    await actors.Ask<bool>(actors.Volunteers, new DeleteEvent(id));
    return Results.Redirect("/calendar");
}).RequireAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>();

app.Run();
