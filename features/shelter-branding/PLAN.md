# Shelter Branding Settings — Implementation Plan

## 1. Feature Summary

A Manager-only settings page lets the shelter configure its own branding: a logo image (uploaded file), a shelter name, and a tagline/phrase shown beneath the name. These three values are persisted in a single configuration row and rendered in the sidebar header (top-left) of every page, replacing the currently hardcoded, localized App_Name / App_Branch strings and the static pets icon in MainLayout.razor. Volunteers can see the branding but cannot change it; only Managers reach the settings form.

## 2. User Flow

1. A Manager opens the sidebar and clicks the new **Settings** nav link (visible only inside an AuthorizeView with Roles=Manager). Route: GET /settings. Page requires [Authorize]; the form actions require the Manager policy.
2. The page (Settings.razor) loads the current ShelterSettings row via ShelterApiClient.GetSettings() (falls back to seeded defaults — a row always exists). It renders two areas:
   - **Logo upload** card (mirrors the dog photo card in DogEdit.razor): a preview of the current logo (or the pets icon fallback) plus a multipart/form-data POST form to POST /api/settings/logo.
   - **Name + tagline** form: text inputs for name and phrase, a single form (method=post, formname=branding) plus an AntiforgeryToken, the handler read in OnInitializedAsync.
3. **Logo upload action:** submitting the file form POSTs to /api/settings/logo (RequireAuthorization Manager + DisableAntiforgery, exactly like /api/dogs/{id}/photo). The endpoint validates extension + size, writes the file to wwwroot/branding/, tells the actor the new path, then Results.Redirect to /settings (POST-then-redirect).
4. **Name/tagline action:** the in-page form handler reads fields via FormReader, accumulates a List of string _errors, validates (name required), and on success calls ShelterApiClient.UpdateSettings(...) then Nav.NavigateTo to /settings (POST-then-redirect to stop re-submit). On error it re-renders with sticky values and the error list above the form.
5. After either action, the next render of any page reads the updated row and the sidebar header reflects the new logo/name/phrase.

SSR constraints honored: full HTTP round-trips, server-side validation only, POST-then-redirect, antiforgery on the in-page form, DisableAntiforgery only on the file upload (matching the dog-photo precedent).

## 3. Architecture & Layer Placement

Dependency flow respected: Domain to Infrastructure to Application to Web. No new backward dependency is introduced.

| Type / change | Layer | Rationale |
|---|---|---|
| ShelterSettings entity | Refugio.Domain/Entities/ShelterSettings.cs | Pure entity, zero deps — same as Goal, Dog. |
| DbSet ShelterSettings Settings (no query filter — see section 5) | Refugio.Infrastructure/Data/ShelterDbContext.cs | EF mapping lives in Infrastructure. |
| EF migration AddShelterSettings | Refugio.Infrastructure/Migrations/ | Any schema change needs a migration (skill ef-migration). |
| Default row seed | Refugio.Infrastructure/Data/SeedData.cs | Seeding lives with the DbContext; mirrors existing db.Goals.AddRange. |
| ISettingsMessage marker + GetSettings/UpdateSettings/UpdateSettingsLogo records | Refugio.Application/Messages/ | Request messages + marker routing (skill add-actor-message). |
| SettingsActor : ShelterActorBase | Refugio.Application/Actors/SettingsActor.cs | New domain area, new actor (skill add-actor). See section 4. |
| Register SettingsActor + resolve ref + route marker | ShelterSupervisorActor.cs, ShelterActorService.cs | Wire the new actor into the hierarchy + router. |
| SettingsEndpoints.cs (REST verbs + POST-for-form + logo upload) | Refugio.Web/Endpoints/SettingsEndpoints.cs | Endpoints live in Web; one file per domain area. |
| ShelterApiClient settings methods | Refugio.Web/Services/ShelterApiClient.cs | Thin facade over actors used by Blazor. |
| Settings.razor page (/settings) | Refugio.Web/Components/Pages/Settings.razor | SSR form page (skill blazor-ssr-form-page). |
| MainLayout.razor header change + Settings nav link | Refugio.Web/Components/Layout/MainLayout.razor | Render branding from DB; nav entry. |
| RESX keys (4 cultures) | Refugio.Web/Resources/SharedResources*.resx | All visible strings localized (skill add-localization). |
| Unit + integration tests | tests/Refugio.Tests, tests/Refugio.Tests.Integration | Actor + endpoint + RBAC + render coverage. |

## 4. Actor Model Design

**Owning actor: a NEW SettingsActor : ShelterActorBase.** Justification: branding/settings is a distinct domain area with its own entity and lifecycle. It does not belong to Dogs/Finance/Adoption/Volunteer/Task semantics; bolting it onto FinanceActor (where Goal lives) would violate single-responsibility and confuse marker routing. A new actor keeps the one-actor-per-area convention intact and mirrors the existing actors exactly. The actor extends ShelterActorBase(IServiceScopeFactory) like all five existing actors.

New marker interface in Messages/ShelterMessage.cs:

    public interface ISettingsMessage : IShelterMessage { }

Messages (new Messages/SettingsMessages.cs):

    public record GetSettings() : ISettingsMessage;
    public record UpdateSettings(string Name, string? Phrase) : ISettingsMessage;
    public record UpdateSettingsLogo(string LogoUrl) : ISettingsMessage;

ShelterSettings (the response of GetSettings/UpdateSettings) is NOT a marker — only requests route.

SettingsActor ctor registers three handlers (same shape as the FinanceActor Goal handlers):

    ReceiveAsync<GetSettings>(Handle);
    ReceiveAsync<UpdateSettings>(Handle);
    ReceiveAsync<UpdateSettingsLogo>(Handle);

Handler bodies use the ShelterActorBase.WithDb scope-per-handler pattern. A private GetOrCreate helper guarantees the single row exists (the seed creates it, but tests using EnsureCreated without seed need it too):

    private Task Handle(GetSettings msg) => WithDb(async db =>
        Sender.Tell(await GetOrCreate(db)));

    private Task Handle(UpdateSettings msg) => WithDb(async db =>
    {
        var s = await GetOrCreate(db);
        s.Name = msg.Name;
        s.Phrase = msg.Phrase;
        await db.SaveChangesAsync();
        Sender.Tell(s);
    });

    private Task Handle(UpdateSettingsLogo msg) => WithDb(async db =>
    {
        var s = await GetOrCreate(db);
        s.LogoUrl = msg.LogoUrl;
        await db.SaveChangesAsync();
        Sender.Tell(true);
    });

    private static async Task<ShelterSettings> GetOrCreate(ShelterDbContext db)
    {
        var s = await db.Settings.FirstOrDefaultAsync();
        if (s is null)
        {
            s = new ShelterSettings { Name = "Haven Sanctuary", Phrase = "City Main Branch" };
            db.Settings.Add(s);
            await db.SaveChangesAsync();
        }
        return s;
    }

Response types: GetSettings -> ShelterSettings; UpdateSettings -> ShelterSettings; UpdateSettingsLogo -> bool.

The only logic intentionally outside the actor is the **file-system write** for the logo image, which stays in the endpoint exactly as the dog-photo upload does (IWebHostEnvironment.WebRootPath, disk write); the actor only persists the resulting URL string. This matches the established precedent and keeps file I/O out of the actor.

## 5. Data Model & Migration

New entity Refugio.Domain/Entities/ShelterSettings.cs:

    public class ShelterSettings : ISoftDeletable
    {
        public int Id { get; set; }
        public string Name { get; set; } = "Haven Sanctuary";
        public string? Phrase { get; set; }
        public string? LogoUrl { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

**ISoftDeletable decision:** implement the interface for consistency with every other entity, BUT do NOT add a global query filter for ShelterSettings in OnModelCreating, and do NOT expose any delete/restore message or /admin/deleted tab. Rationale: this is a singleton configuration row, not user content — there is no delete-branding use case, and a query filter would risk the layout query returning null if the row were ever soft-deleted. Implementing the interface (so Id + DeletedAt exist) costs nothing; omitting the filter and delete UX is the deliberate exception. This is the one place we knowingly diverge from the all-entities-get-a-filter convention, justified by the singleton-config nature of the row.

Mapping in ShelterDbContext: add a DbSet ShelterSettings Settings => Set ShelterSettings (). No HasConversion (no enums), no decimal columns, **no HasQueryFilter** (see above).

Seed in SeedData.Seed (inside the existing guard that returns early when db.Dogs.Any(), alongside db.Goals.AddRange):

    db.Settings.Add(new ShelterSettings { Name = "Haven Sanctuary", Phrase = "City Main Branch" });

Migration: dotnet ef migrations add AddShelterSettings --project src/Refugio.Infrastructure --startup-project src/Refugio.Web (skill ef-migration). Applied automatically at startup via db.Database.Migrate(). The seed only runs on a fresh DB; for an already-seeded DB the GetOrCreate helper in the actor backfills the row on first read.

## 6. SOLID & Clean Architecture Notes

- **Single Responsibility:** SettingsActor owns only settings persistence; the endpoint owns only HTTP + file I/O; Settings.razor owns only presentation/validation. No god-class growth on FinanceActor.
- **Open/Closed:** the feature extends the system purely additively through the marker-interface router — adding the ISettingsMessage -> Settings case to ShelterActorService.Route is the single open/closed extension point; no existing handler is modified.
- **Liskov / Interface Segregation:** SettingsActor extends ShelterActorBase and uses only the WithDb slice it needs; ShelterSettings implements ISoftDeletable consistently even though we opt out of the filter — the interface contract is still honored.
- **Dependency Inversion:** Blazor depends on the ShelterApiClient facade, which depends on the ShelterActorService abstraction (in-process Ask of T), not on the actor type directly. MainLayout obtains branding through this same facade, not by reaching into EF.
- **Clean Architecture flow:** entity in Domain, EF/migration/seed in Infrastructure, actor/messages in Application, page/endpoints/facade in Web. Web to Application to Infrastructure to Domain only; nothing points backward.

## 7. Step-by-Step Implementation Plan

Ordered by layer dependency: Domain first, Web last. Each step names the file(s)/skill and a verify action. Build after each layer; full dotnet test at the end.

1. **Domain entity.** Create src/Refugio.Domain/Entities/ShelterSettings.cs (fields per section 5, implements ISoftDeletable). Verify: dotnet build of Refugio.Domain.
2. **DbContext mapping.** In src/Refugio.Infrastructure/Data/ShelterDbContext.cs add the DbSet for Settings. Do NOT add a HasQueryFilter. Verify: build Infrastructure.
3. **Seed default row.** In src/Refugio.Infrastructure/Data/SeedData.cs add the db.Settings.Add(...) line before the final db.SaveChanges(). Verify: build Infrastructure.
4. **EF migration.** Run skill ef-migration then add a migration named AddShelterSettings against Refugio.Infrastructure with startup project Refugio.Web. Verify: migration file + snapshot updated; build succeeds.
5. **Marker interface.** In src/Refugio.Application/Messages/ShelterMessage.cs add interface ISettingsMessage extending IShelterMessage. Verify: build Application.
6. **Messages.** Create src/Refugio.Application/Messages/SettingsMessages.cs with GetSettings, UpdateSettings, UpdateSettingsLogo (skill add-actor-message). Verify: build.
7. **Actor.** Create src/Refugio.Application/Actors/SettingsActor.cs extending ShelterActorBase with the three handlers and the GetOrCreate helper (skill add-actor). Verify: build.
8. **Supervisor registration.** In ShelterSupervisorActor.cs register a SettingsActor child named settings. Verify: build.
9. **Service router + ref.** In ShelterActorService.cs add a public IActorRef Settings property, resolve /user/shelter/settings in the ctor, and add the ISettingsMessage to Settings case in Route. Verify: build Application; unit tests in step 16 confirm routing.
10. **Facade methods.** In src/Refugio.Web/Services/ShelterApiClient.cs add GetSettings, UpdateSettings(name, phrase), UpdateSettingsLogo(url) routing the three messages. Verify: build Web.
11. **Endpoints.** Create src/Refugio.Web/Endpoints/SettingsEndpoints.cs with MapSettingsEndpoints: GET /settings (REST read, auth), PUT /settings (REST update, Manager), POST /settings/update (form to update to redirect /settings, Manager), POST /settings/logo (multipart, validate ext/size, write to wwwroot/branding/logo with the file extension, delete any old logo file, call UpdateSettingsLogo, redirect /settings; RequireAuthorization Manager plus DisableAntiforgery — mirrors /api/dogs/{id}/photo). Register MapSettingsEndpoints in Program.cs after MapVolunteerEndpoints. Verify: build; integration test for redirect + RBAC.
12. **Settings page.** Create src/Refugio.Web/Components/Pages/Settings.razor at /settings, with the Authorize attribute (skill blazor-ssr-form-page): load via GetSettings, logo card (preview plus multipart form to /api/settings/logo), name/phrase form (method post, formname branding) plus an AntiforgeryToken, read in OnInitializedAsync via IHttpContextAccessor and FormReader, _errors validation (name required via Validator.RequireNotEmpty), POST-then-redirect with Nav.NavigateTo. The page is gated by the Manager-policy endpoints. Verify: build; render as Manager.
13. **MainLayout — render branding from DB.** See section 8. Replace the App_Name / App_Branch localizer usages with the fetched Name / Phrase; render an img tag whose src is LogoUrl when set, else keep the pets icon. Add a Manager-only AuthorizeView Settings NavLink (icon settings, label from Nav_Settings). Verify: run app, confirm sidebar shows seeded values and updates after a save.
14. **Localization.** Run skill add-localization to add keys to ALL FOUR RESX files (SharedResources.resx, .es-ES, .pt-BR, .ca-ES): Nav_Settings, Settings_Title, Settings_Name, Settings_Phrase, Settings_Logo, Settings_UploadLogo, Settings_ChangeLogo, Settings_LogoHint ("Recommended square image; shown at 100x100."), Settings_Save, Settings_NameRequired, Settings_Saved. Keep App_Name/App_Branch as the layout fallback default. Verify: build; switch each language, confirm no raw key strings appear.
14b. **App.razor static title.** Change the title element literal in src/Refugio.Web/Components/App.razor from "Haven Sanctuary" to "Refugio" (static, NOT data-driven — see section 10). Verify: build; browser tab reads "Refugio".
15. **Logo storage dir.** Ensure wwwroot/branding/ is created by the endpoint via Directory.CreateDirectory. Verify: upload a logo, confirm the file lands in src/Refugio.Web/wwwroot/branding/ and the preview shows.
16. **Unit tests** (section 9). Verify: dotnet test of Refugio.Tests.
17. **Integration tests** (section 9). Verify: dotnet test of Refugio.Tests.Integration.
18. **Full build + test.** dotnet build then dotnet test. Verify: all green; existing 142 unit / 103 integration counts increase, none regress.

## 8. MainLayout Data Fetch (SSR mechanism)

MainLayout.razor today reads everything synchronously in a Razor code block from IHttpContextAccessor and IStringLocalizer — it never hits the DB or actor. To render dynamic branding it must fetch the settings row per render, the same way pages do, via the scoped ShelterApiClient:

- Inject Refugio.Web.Services.ShelterApiClient (as Api) into MainLayout.razor.
- Override OnInitializedAsync (layouts support lifecycle methods in SSR): call await Api.GetSettings() and store Name/Phrase/LogoUrl in fields.
- Fallback: if the row is null, fall back to the App_Name / App_Branch localizer values and the pets icon. With the seed plus GetOrCreate backfill the row always exists, so the fallback is belt-and-suspenders.
- **Logo render (fixed 100x100, scaled to fit):** when LogoUrl is set, render an img inside a fixed 100x100 box that scales the source to fit — no server-side resize. Tailwind: `<img src="@LogoUrl" class="w-[100px] h-[100px] object-cover" alt="logo" />` (object-cover = crop to fill, preserves aspect; swap to object-fill for stretch). When LogoUrl is null, keep the existing pets icon at the same 100x100 footprint. The same 100x100 box is reused for the preview in the Settings page logo card.
- Cost: one in-process Ask per page render. Acceptable at this scale and consistent with how every page already calls the actor system per render. If profiling ever flags it, an IMemoryCache invalidated on UpdateSettings is the documented future optimization — not implemented now to avoid premature complexity.

This keeps MainLayout on the same dependency-inversion path as the pages (layout to facade to actor), not reaching into EF directly.

## 9. Testing Strategy

**Unit tests** — tests/Refugio.Tests/Actors/SettingsActorTests.cs (Akka.TestKit plus EF in-memory, extend ActorTestBase):

- GetSettings_ReturnsSeededOrCreatedRow — first GetSettings on an empty in-memory DB returns a non-null row with the default name (GetOrCreate path).
- UpdateSettings_PersistsNameAndPhrase — send UpdateSettings with a new name and phrase, expect the returned row and a re-read GetSettings to reflect the change.
- UpdateSettings_AllowsNullPhrase — phrase may be cleared to null.
- UpdateSettingsLogo_PersistsUrl — send UpdateSettingsLogo with a path, expect true and the URL persisted.
- SingleRow_NeverDuplicated — repeated GetSettings/UpdateSettings calls keep exactly one row (Settings count stays at 1).

**Integration tests** — tests/Refugio.Tests.Integration/SettingsApiTests.cs (WebApplicationFactory plus SQLite in-memory, IClassFixture of ShelterWebFactory):

- GetSettings_Authenticated_ReturnsOk — GET /api/settings as a logged-in user returns 200 with the seeded name.
- UpdateSettings_AsManager_Redirects — POST /api/settings/update (form, antiforgery) as Manager updates and redirects to /settings.
- UpdateSettings_AsVolunteer_Forbidden — RBAC: Volunteer is blocked on the Manager-gated update.
- LogoUpload_AsManager_StoresFileAndUpdatesUrl — multipart POST to /api/settings/logo returns redirect; subsequent GET /api/settings shows a /branding/... URL. Use a small in-memory PNG byte array; assert validation rejects a .txt.
- LogoUpload_AsVolunteer_Forbidden — RBAC on the logo endpoint.
- SettingsPage_RendersForManager — GET /settings returns 200 and HTML containing the settings form (mirrors ReportsApiTests page-render checks).

**No E2E/browser tests** — SSR pages are fully covered by the integration request pipeline (per project convention).

## 10. Risks, Tradeoffs & Open Questions

- **Per-render actor call from the layout.** Every page now does one extra Ask for branding. Low cost at this scale; caching is the documented future optimization. Confirm this is acceptable or whether an IMemoryCache is wanted from day one.
- **Soft-delete exception.** ShelterSettings implements ISoftDeletable but is deliberately excluded from the global query filter and from /admin/deleted. This is the one intentional divergence from the universal-filter convention; flagged for sign-off.
- **Single-row enforcement is by convention, not schema.** GetOrCreate plus always editing the first row keeps it singleton, but nothing in the DB forbids a second row. Acceptable since only the actor writes it. A unique-singleton guard could be added if hard enforcement is wanted.
- **Logo file lifecycle.** Old logo files are overwritten/deleted by filename pattern (like dog photos). Orphaned files from a failed write are not garbage-collected — the same tradeoff the dog-photo upload already accepts.
- **RESOLVED — App.razor title stays static.** The browser-tab title in App.razor does NOT follow the configured name. Change the static title element value from "Haven Sanctuary" to **"Refugio"** (a one-line literal edit in App.razor). Only the sidebar header (MainLayout) is dynamic. No data-fetch added to App.razor.
- **RESOLVED — logo is fixed 100x100, scaled to fit via CSS.** The logo always renders in a fixed 100x100 px box. We do NOT re-encode/resize the uploaded file server-side (the project has no image library and System.Drawing/ImageSharp would be a new dependency); instead the stored original is displayed in a 100x100 container and CSS scales it to fit. Default `object-fit: cover` (crop to fill 100x100, preserves aspect, no distortion); `object-fit: fill` (stretch) is the one-token alternative if exact non-cropped stretch is preferred. See section 8 for the markup. Upload validation keeps the dog-photo rules (5 MB max; jpg/png/webp). Settings_LogoHint text: "Recommended square image; shown at 100x100." (4 cultures).
