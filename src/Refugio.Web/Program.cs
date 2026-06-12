using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Refugio.Application;
using Refugio.Domain.Helpers;
using Refugio.Infrastructure;
using Refugio.Infrastructure.Data;
using Refugio.Web.Components;
using Refugio.Web.Endpoints;
using Refugio.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ShelterDbContext>(opt =>
    opt.UseSqlite("Data Source=shelter.db"));

// DDD layers: application use cases + infrastructure adapters (repos, UoW, queries, email).
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();

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

// The Blazor SSR UI consumes the REST API over HTTP. The named client forwards the
// caller's cookies (auth + culture) and never keeps its own cookie jar.
builder.Services.AddTransient<ForwardCookieHandler>();
builder.Services.AddHttpClient(ShelterApiClient.ClientName)
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        UseCookies = false,
        AllowAutoRedirect = false
    })
    .AddHttpMessageHandler<ForwardCookieHandler>();
builder.Services.AddScoped<ShelterApiClient>();

builder.Services.AddHostedService<AppointmentReminderService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<SettingsCacheService>();

var app = builder.Build();

DatabaseInitializer.Initialize(app.Services);

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

// REST API — grouped by domain area; endpoints call the application services.
var api = app.MapGroup("/api");
api.MapApiAuthEndpoints();
api.MapDogEndpoints();
api.MapAdoptionEndpoints();
api.MapTaskEndpoints();
api.MapFinanceEndpoints();
api.MapVolunteerEndpoints();
api.MapSettingsEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>();

// Serve the built React SPA from ClientApp/dist when it exists.
// The dist/ directory is produced by `npm run build`; it is not present in source.
var spaDist = Path.Combine(builder.Environment.ContentRootPath, "ClientApp", "dist");
if (Directory.Exists(spaDist))
{
    var spaProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(spaDist);
    app.UseStaticFiles(new StaticFileOptions { FileProvider = spaProvider, RequestPath = "" });
    app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = spaProvider });
}

app.Run();

public partial class Program { }
