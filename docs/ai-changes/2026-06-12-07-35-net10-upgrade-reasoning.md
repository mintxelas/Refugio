# net10.0 solution upgrade — reasoning

## Goal

Upgrade all 7 projects from `net9.0` to `net10.0` and bump Microsoft.* package
version constraints from `9.*` to `10.*`.

## Step-by-step logic

1. **Inventory.** Glob all `.csproj` files; read each. Confirmed 7 projects all on `net9.0`.
   No `global.json` exists, so no SDK pin to update.

2. **Identify package version constraints.** Only `Microsoft.*` packages carry a `9.*` float.
   Akka.Hosting 1.5.68 and Akka.TestKit.Xunit2 1.5.68 target netstandard2.0 / net6+ —
   no change needed, they load on net10 without modification.

3. **Apply changes in parallel.** All 7 `TargetFramework` edits made concurrently; then all
   `9.*` → `10.*` replacements on the 4 projects that carry such references.

4. **Build.** `dotnet build` — 0 errors. 16 warnings: all `NU1902` for `OpenTelemetry.Api
   1.10.0` (moderate vulnerability). These are pre-existing transitive warnings pulled in by
   Akka.Hosting and unrelated to the TFM change.

5. **Run tests.** 225 unit pass. 1 integration failure:
   `SettingsApiTests.UpdateSettings_AsAnon_RedirectsToLogin` — expected `302 Found`, got
   `401 Unauthorized`.

6. **Root-cause the failure.** ASP.NET Core 10 changed cookie authentication challenge
   behavior for API requests: a PUT with `application/json` content no longer redirects to the
   login page — it returns 401 directly. This is the correct behavior for an API endpoint.
   The test asserted the old (arguably wrong) behavior. All other auth-redirect tests hitting
   GET or browser-style endpoints were unaffected.

7. **Fix.** Renamed the test to `UpdateSettings_AsAnon_Returns401` and updated the assertion
   to `HttpStatusCode.Unauthorized`. No production code change needed.

8. **Re-run tests.** 225 unit + 149 integration, 0 failures.
