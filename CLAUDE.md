# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build entire solution
dotnet build

# Run the web app (http://localhost:5110)
cd src/Refugio.Web && dotnet run

# Build a specific project
dotnet build src/Refugio.Web/Refugio.Web.csproj

# Restore packages
dotnet restore
```

There are no tests yet. The SQLite database (`shelter.db`) is created automatically on first run inside `src/Refugio.Web/` with seed data. Schema migrations are done with try/catch `ALTER TABLE ... ADD COLUMN` statements at startup in `Program.cs`.

## Architecture

Four projects with a strict dependency flow:

```
Domain → Infrastructure → Application → Web
```

- **Refugio.Domain** — Pure entity classes and enums. No dependencies. Entities: `Dog`, `MedicalRecord`, `Medication`, `Adoption`, `ShelterTask`, `Donation`, `Expense`, `Volunteer`, `ShelterEvent`.
- **Refugio.Infrastructure** — EF Core + SQLite (`ShelterDbContext`). `SeedData.Seed()` populates the DB on first run.
- **Refugio.Application** — Akka.NET actor system. Each domain area has a dedicated actor (`DogActor`, `AdoptionActor`, `FinanceActor`, `VolunteerActor`, `TaskActor`). `ShelterSupervisorActor` spawns all child actors. `ShelterActorService` is the singleton bridge between ASP.NET DI and the actor hierarchy.
- **Refugio.Web** — Hosts both the REST API (minimal API, `/api/*`) and Blazor SSR pages. `ShelterApiClient` (scoped) calls `ShelterActorService` (singleton) directly — no HTTP round-trip between Blazor pages and the API.

## Akka.NET actor pattern

**Critical:** Actors are singleton-lifetime but `ShelterDbContext` is scoped. Every actor receives `IServiceScopeFactory` (not `DbContext`) and creates a scope per message handler:

```csharp
public class SomeActor : ReceiveActor
{
    private readonly IServiceScopeFactory _scopeFactory;
    public SomeActor(IServiceScopeFactory scopeFactory) { ... }

    private async Task Handle(SomeMessage msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShelterDbContext>();
        // use db, scope disposes after handler
    }
}
```

`ShelterActorService` uses `Task.Delay(500).Wait()` after supervisor creation + 5-second `ResolveOne` timeout to avoid a startup race condition.

Adding a new actor: implement `ReceiveActor`, register it in `ShelterSupervisorActor` constructor, expose a ref in `ShelterActorService`, add convenience methods to `ShelterApiClient`.

## API + Blazor integration

API endpoints are declared in `Program.cs` via `app.MapGroup("/api")`. Blazor pages inject `ShelterApiClient` and call it directly (actor `Ask<T>` calls, 10 s timeout). There is no `HttpClient` — Blazor pages talk to actors in-process.

## Blazor SSR — critical constraints

This app uses **pure static SSR** (no interactive render mode). `@onclick`, `@bind`, and other interactive Razor directives are silently ignored. Use only SSR-compatible patterns:

- **Forms:** `<form method="post" @formname="name">` + `<AntiforgeryToken />`. Read form data in `OnInitializedAsync` via `IHttpContextAccessor` — do NOT use `[SupplyParameterFromForm]`, it silently fails when any form field cannot bind to its property type (e.g. empty string → `int`).
  ```csharp
  @inject IHttpContextAccessor HttpContextAccessor

  protected override async Task OnInitializedAsync()
  {
      var ctx = HttpContextAccessor.HttpContext;
      if (ctx?.Request.Method == "POST")
      {
          var form = await ctx.Request.ReadFormAsync();
          // read form["FieldName"].ToString()
      }
  }
  ```
- **Navigation/filters:** `[SupplyParameterFromQuery]` for query params + `<a href="/page?param=x">` links (no JS).
- **Toggles:** `<details>/<summary>` for collapsible sections (no JS).
- **Full-row clickable table rows:** absolute-positioned `<a class="absolute inset-0">` inside a `relative` container. Content divs use `pointer-events-none` so clicks pass through to the link. Only interactive child elements (buttons, action links) get `relative z-10`.
- **POST-then-redirect:** after form processing call `Nav.NavigateTo(...)` to redirect — this triggers a 302, preventing re-submission on refresh.
- **SSR-compatible activate/deactivate/delete:** `GET /api/resource/{id}/activate` (and `/delete`) endpoints that redirect back to the listing page after the action. Use `.RequireAuthorization()` on these.
- **Multiple forms on one page:** check `form["_handler"].ToString()` (the value injected by `@formname`) to distinguish which form was submitted when a page has more than one `<form>`.
- **Tabs via query params:** use `[SupplyParameterFromQuery]` + `<a href="/page?tab=x">` links. Active tab styling via `Tab == "x"` condition.

## Authentication

Cookie-based auth (`CookieAuthenticationDefaults`). Login: `POST /auth/login`, logout: `GET /auth/logout`, change password: `POST /auth/change-password`. All three use `.DisableAntiforgery()`.

All Blazor pages carry `@attribute [Authorize]`. Unauthenticated requests are redirected to `/login` via `<AuthorizeRouteView>` + `<RedirectTo>` component in `Routes.razor`.

Password hashing: PBKDF2-SHA256 via `PasswordHelper` in `Refugio.Domain.Helpers` (no external packages).

`Volunteer` entities have `CanLogin` (bool) and `PasswordHash` (string?) fields. The seed volunteer `elena@havensanctuary.org` / `shelter123` has login access.

`MainLayout.razor` reads user claims via `IHttpContextAccessor` to display the username and a hover dropdown with Change Password and Sign Out links.

## Styling

Tailwind CSS loaded via CDN in `App.razor`. The design token config (colors, spacing, fonts) is defined inline in the `<script>` block in `App.razor`. All colors follow the `primary/secondary/tertiary/surface-*` naming from Material Design 3. Do not use arbitrary hex values — use the named tokens.
