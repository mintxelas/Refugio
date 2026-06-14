# Remove Dead Code — Reasoning

## Goal
Detect and remove dead code across the Refugio solution.

## Step-by-step logic

1. **Tried compiler warnings first.** Full rebuild only surfaced `NU1902`
   (OpenTelemetry vulnerability) warnings — no unused-member signal, because the
   project ships with the default analyzer severities (unused-member rules are
   `suggestion`, not emitted at build).

2. **Tried codegraph structural query.** Queried `.codegraph/codegraph.db`
   directly for symbols in `src/` with zero incoming `calls/instantiates/extends/
   references` edges. Result: 423 candidates — far too many. Inspection showed the
   C# call resolver misses extension-method calls (`.ToDto()`), interface dispatch,
   DI-resolved services, EF overrides, and Akka message handlers. Conclusion: the
   structural graph is too lossy for confident C# dead-code removal.

3. **Cross-checked with a textual filter.** For each zero-edge candidate, counted
   word-boundary occurrences of its name across all `.cs/.ts/.tsx` sources. Only
   names appearing once survived — all of which were framework entry points
   (`OnModelCreating`, `OnConfiguring`, `*Endpoints` registration classes,
   `DtoMapping`). This **proved there are no truly dead C# symbols**.

4. **Used Roslyn for the authoritative private-member check.** Dropped a temporary
   root `.editorconfig` elevating `IDE0051`/`IDE0052`/`CS0414` to `warning` and
   rebuilt with `EnforceCodeStyleInBuild=true`. Exactly one hit:
   `SettingsApiTests.VolunteerClientAsync` — an unused private test helper.

5. **Scanned for deleted-feature leftovers.** Confirmed zero `.razor`/`.cshtml`,
   zero `.resx`, and zero references to the June-2026-deleted Blazor types
   (`ShelterApiClient`, `FormReader`, `Validator`, etc.). The C# side is clean.

6. **Scanned tracked static assets.** Found two classes of dead artifacts:
   - `coverage/` — 135 generated HTML coverage-report files (3.4 MB) committed to
     git; `.gitignore` only excluded `coverage*.json/xml/info`, not the HTML dir.
   - `wwwroot/lib/bootstrap/**` (44 files) and `wwwroot/app.css` — Blazor-era
     assets with **zero references** anywhere outside `wwwroot` (the SPA uses the
     Tailwind CDN in `ClientApp/index.html`).

## What was removed
- `tests/.../SettingsApiTests.cs` → unused `VolunteerClientAsync` helper.
- `coverage/` directory (untracked from git) + added `coverage/` to `.gitignore`.
- `src/Refugio.Web/wwwroot/lib/` (Bootstrap) and `wwwroot/app.css`.

## What was deliberately kept
- `wwwroot/dogs/1.png` — sits under the runtime photo-upload root used by
  `PhotoFiles`; treated as data, not dead code.
- `wwwroot/favicon.png` — ambiguous (no `<link>` reference, but harmless default);
  left in place rather than risk a visible regression.

## Alternatives rejected
- **Trusting the 423 codegraph candidates** — rejected: ~98% false positives from
  unresolved C# call edges.
- **Committing the elevated `.editorconfig`** — rejected to keep the change focused
  on removal; the temp file was deleted after use.
