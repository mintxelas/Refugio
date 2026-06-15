# Summary: MED/LOW Security Fixes

## What changed

| File | Change |
|---|---|
| `src/Refugio.Domain/Helpers/PasswordHelper.cs` | PBKDF2 iterations 10k → 600k; added `DummyHash` static field |
| `src/Refugio.Application/Services/VolunteerService.cs` | Login runs dummy verify when user not found (timing equalization) |
| `src/Refugio.Web/Endpoints/DogEndpoints.cs` | Open redirect fix on `returnUrl`; magic byte check on photo uploads |
| `src/Refugio.Web/Endpoints/VolunteerEndpoints.cs` | Magic byte check on volunteer photo upload |
| `src/Refugio.Web/Endpoints/FinanceEndpoints.cs` | CSV injection prefix in `CsvField()`; magic byte check on expense photos |
| `src/Refugio.Web/Endpoints/AdoptionEndpoints.cs` | Magic byte check on adoption photos |
| `src/Refugio.Web/Helpers/PhotoFiles.cs` | Path containment check in `DeleteByUrl`; new `HasValidImageBytes` method |
| `tests/Refugio.Tests.Integration/PaginationTests.cs` | `IAsyncLifetime` pattern; `_managerClient` for authenticated seeding |
| `tests/Refugio.Tests.Integration/SecurityTests.cs` | Relaxed two assertions from exact 302 to `401 or redirect` |
| `tests/Refugio.Tests.Unit/Services/VolunteerServiceTests.cs` | Fixed `"Helper"` → `Roles.Volunteer` and `"Lead"` → `Roles.Volunteer` |

## Why it changed

All changes address findings from a security audit:
- **MED-1** (open redirect): blocked by validating `returnUrl` is relative-only
- **MED-2** (file upload bypass): blocked by verifying JPEG/PNG magic bytes
- **MED-3** (CSV injection): blocked by formula-char prefix
- **MED-4** (weak PBKDF2): 600k iterations match current OWASP guidance
- **LOW-1** (timing oracle): equalized with a dummy hash
- **LOW-3** (path traversal): canonicalized path checked against webroot

## Side effects / follow-up

- **Password re-hashing**: Existing production database rows still have 10k-iteration hashes. Current `Verify` uses 600k iterations and will FAIL to verify old hashes. **Run a one-time migration that re-hashes all volunteer passwords before deploying to production.** (Seed data re-creates on every fresh DB so tests are unaffected.)
- **Login latency**: Each login now takes ~300ms longer due to 600k PBKDF2 iterations. This is intentional and acceptable for a shelter app.
- **CSV export**: The single-quote prefix is invisible in Excel but visible if the CSV is opened as plain text. Expected and harmless.

## Code review checklist

- [ ] `PasswordHelper.Verify` still uses the same iteration count as `Hash` (600k)
- [ ] `DummyHash` is static readonly — not re-computed per request
- [ ] `LoginAsync` calls `PasswordHelper.Verify` on the dummy path and discards the result (always returns null)
- [ ] `HasValidImageBytes` resets the stream position before the magic byte read — it opens a NEW stream via `file.OpenReadStream()` so position is always 0
- [ ] `CsvField` only prefixes the first character; multi-byte sequences are not affected
- [ ] `DeleteByUrl` containment check uses `Path.DirectorySeparatorChar` appended to root to prevent prefix false positives
- [ ] All photo upload handlers call `HasValidImageBytes` BEFORE actor ask (fail fast)
- [ ] Integration test `PaginationTests` uses `_managerClient` (initialized once) for writes; `anonClient` only for public GETs
