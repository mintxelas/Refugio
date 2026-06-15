using Microsoft.AspNetCore.Authentication.Cookies;
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
app.UseAuthentication();
app.UseAuthorization();

var api = app.MapGroup("/api");
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
