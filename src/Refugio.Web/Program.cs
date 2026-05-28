using Akka.Actor;
using Akka.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
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
    db.Database.EnsureCreated();
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Volunteers ADD COLUMN CanLogin INTEGER NOT NULL DEFAULT 0"); } catch { }
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Volunteers ADD COLUMN PasswordHash TEXT"); } catch { }
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
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

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
