using Akka.Actor;
using Akka.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Refugio.Application.Services;
using Refugio.Domain.Helpers;
using Refugio.Infrastructure.Data;
using Refugio.Web.Components;
using Refugio.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ShelterDbContext>(opt =>
    opt.UseSqlite("Data Source=shelter.db"));

builder.Services.AddLocalization(opt => opt.ResourcesPath = "Resources");

var supportedCultures = new[] { new CultureInfo("en-US"), new CultureInfo("es-ES"), new CultureInfo("pt-BR"), new CultureInfo("ca-ES") };
builder.Services.Configure<RequestLocalizationOptions>(opts =>
{
    opts.DefaultRequestCulture = new RequestCulture("en-US");
    opts.SupportedCultures = supportedCultures;
    opts.SupportedUICultures = supportedCultures;
    opts.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
});

builder.Services.AddRazorComponents();
builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    opts.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.LoginPath = "/login";
        opt.AccessDeniedPath = "/";
        opt.ExpireTimeSpan = TimeSpan.FromDays(7);
        opt.SlidingExpiration = true;
    });
builder.Services.AddAuthorization(opts =>
    opts.AddPolicy("Manager", p => p.RequireRole(Roles.Manager)));
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
builder.Services.AddSingleton<Refugio.Application.Services.IShelterEmailSender, Refugio.Web.Services.NoOpEmailSender>();
builder.Services.AddHostedService<Refugio.Web.Services.AppointmentReminderService>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<Refugio.Web.Services.SettingsCacheService>();

var app = builder.Build();

DatabaseInitializer.Initialize(app.Services);

_ = app.Services.GetRequiredService<ShelterActorService>();

if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Auth + language switcher (operate on the app root, not the /api group)
app.MapAuthEndpoints(supportedCultures);

// REST API — grouped by domain area; each message routes to its owning actor.
var api = app.MapGroup("/api");
api.MapDogEndpoints();
api.MapAdoptionEndpoints();
api.MapTaskEndpoints();
api.MapFinanceEndpoints();
api.MapVolunteerEndpoints();
api.MapSettingsEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>();

app.Run();

public partial class Program { }
