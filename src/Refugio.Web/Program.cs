using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Refugio.Actors;
using Refugio.Application;
using Refugio.Domain.Helpers;
using Refugio.Infrastructure;
using Refugio.Infrastructure.Data;
using Refugio.Web.Endpoints;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ShelterDbContext>(opt =>
    opt.UseSqlite("Data Source=shelter.db"));

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();
builder.Services.AddShelterActors();

builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    opts.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((doc, _, _) =>
    {
        doc.Info.Title = "Refugio Shelter API";
        doc.Info.Version = "v1";
        return Task.CompletedTask;
    });
});

builder.Services.AddCors(opts =>
    opts.AddPolicy("Mobile", p => p
        .WithOrigins("http://localhost:8081", "http://localhost:19006")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.LoginPath = "/login";
        opt.AccessDeniedPath = "/";
        opt.ExpireTimeSpan = TimeSpan.FromDays(7);
        opt.SlidingExpiration = true;
        opt.Cookie.HttpOnly = true;
        opt.Cookie.SecurePolicy = builder.Environment.IsProduction()
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.SameAsRequest;
        opt.Cookie.SameSite = builder.Environment.IsDevelopment()
            ? SameSiteMode.Lax
            : SameSiteMode.Strict;
    });
builder.Services.AddAuthorization(opts =>
    opts.AddPolicy("Manager", p => p.RequireRole(Roles.Manager)));

// CSRF protection: double-submit header pattern. The SPA fetches a token from
// GET /api/antiforgery/token and echoes it back in the X-XSRF-TOKEN header on every
// mutating request; the antiforgery cookie itself stays HttpOnly (JS never reads it).
builder.Services.AddAntiforgery(opts => opts.HeaderName = "X-XSRF-TOKEN");

// Throttle credential-guessing attempts against auth endpoints, per client IP.
// Configurable so integration tests (many logins per run, one shared factory) can raise the limit.
var authRateLimitPermits = builder.Configuration.GetValue("Auth:RateLimitPermitLimit", 5);
var authRateLimitWindow = TimeSpan.FromSeconds(builder.Configuration.GetValue("Auth:RateLimitWindowSeconds", 60));
builder.Services.AddRateLimiter(opts =>
{
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opts.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = authRateLimitPermits,
            Window = authRateLimitWindow,
            QueueLimit = 0,
        }));
});

var app = builder.Build();

DatabaseInitializer.Initialize(app.Services);

// Map domain validation errors (ArgumentException) to 400 Bad Request.
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    context.Response.StatusCode = error is ArgumentException
        ? StatusCodes.Status400BadRequest
        : StatusCodes.Status500InternalServerError;
    if (error is ArgumentException)
        await context.Response.WriteAsJsonAsync(new { error = error.Message });
}));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
    app.UseHsts();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("Mobile");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/api/antiforgery/token", (IAntiforgery antiforgery, HttpContext ctx) =>
    Results.Text(antiforgery.GetAndStoreTokens(ctx).RequestToken!))
    .AllowAnonymous();

// Minimal APIs only auto-validate antiforgery for typed [FromForm]-bound parameters; every
// mutating endpoint here reads JSON bodies (or the raw multipart form manually), so none of
// them trip that automatic check. Validate explicitly for every non-safe method instead.
async ValueTask<object?> ValidateAntiforgery(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
{
    var method = ctx.HttpContext.Request.Method;
    if (!HttpMethods.IsGet(method) && !HttpMethods.IsHead(method) &&
        !HttpMethods.IsOptions(method) && !HttpMethods.IsTrace(method))
    {
        var antiforgery = ctx.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
        try
        {
            await antiforgery.ValidateRequestAsync(ctx.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.StatusCode(StatusCodes.Status400BadRequest);
        }
    }
    return await next(ctx);
}

var api = app.MapGroup("/api").RequireAuthorization().AddEndpointFilter(ValidateAntiforgery);
api.MapApiAuthEndpoints();
api.MapDogEndpoints();
api.MapAdoptionEndpoints();
api.MapTaskEndpoints();
api.MapFinanceEndpoints();
api.MapVolunteerEndpoints();
api.MapSettingsEndpoints();

// In production: serve the React SPA from ClientApp/dist (produced by publish target).
// In development: SpaProxy starts Vite and forwards SPA requests to it automatically.
if (!app.Environment.IsDevelopment())
{
    var spaDist = Path.Combine(builder.Environment.ContentRootPath, "ClientApp", "dist");
    if (Directory.Exists(spaDist))
    {
        var spaProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(spaDist);
        app.UseStaticFiles(new StaticFileOptions { FileProvider = spaProvider, RequestPath = "" });
        app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = spaProvider });
    }
}

app.Run();

public partial class Program { }
