# Reasoning: MED/LOW Security Fixes

## Changes covered

Six medium/low severity vulnerabilities from the security audit, plus follow-up test fixes.

## Step-by-step reasoning

### MED-1: Open redirect (returnUrl)

`/medical/{id}/delete` and `/medications/{id}/delete` accepted a `returnUrl` query parameter and used it directly in `Results.Redirect(returnUrl)`. An attacker could craft a link like `/medical/1/delete?returnUrl=https://evil.com` to redirect a victim after a CSRF delete.

Fix: validate with `Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)` before using the value. Relative-only check is sufficient — relative URLs can't redirect off-site.

### MED-2: File upload magic bytes

`PhotoFiles` only validated file extensions (`.jpg`, `.png`). An attacker could upload a PHP script renamed `.jpg`. `wwwroot` is static-file served, so a web server that processes PHP (or if the file ever moves) would execute it.

Fix: read the first 4 bytes after opening the stream and compare against JPEG (`FF D8 FF`) and PNG (`89 50 4E 47`) magic signatures. Stack-allocate the buffer (`Span<byte>` on stack) to avoid heap allocation.

Applied to: DogEndpoints (two photo upload handlers), VolunteerEndpoints, FinanceEndpoints (expense photos), AdoptionEndpoints.

### MED-3: CSV injection

`FinanceEndpoints.CsvField()` wrote user-supplied strings directly into CSV. A cell starting with `=` is interpreted as a formula by Excel/LibreOffice and can execute macros.

Fix: prefix any cell value whose first character is `= + - @ \t \r` with a single quote. The quote makes the cell a text literal; the formula is never evaluated.

### MED-4: PBKDF2 iteration count

`PasswordHelper` used 10,000 iterations — the OWASP minimum from 2012. Current guidance (OWASP 2024) recommends 600,000 for SHA-256. With 10k, an offline attacker with GPU hardware can crack common passwords in minutes.

Fix: changed the `iterations` constant to `600_000` in both `Hash` and `Verify`. New hashes are created at the higher cost; existing hashes (seed data) are also regenerated at startup since `SeedData.Seed` calls `Volunteer.Register(...)` which re-hashes on every fresh DB.

### LOW-1: Timing oracle in login

`LoginAsync` returned immediately when the email was not found, while a successful lookup ran a `VerifyPassword` call (~300ms with 600k PBKDF2). An attacker measuring response time could distinguish "no such user" from "wrong password", enabling user enumeration.

Fix: added `PasswordHelper.DummyHash` — a static hash computed once at startup. When no candidate is found, `PasswordHelper.Verify(password, DummyHash)` runs the full PBKDF2 work before returning null. This equalizes response time for both branches.

### LOW-3: Path traversal in photo deletion

`PhotoFiles.DeleteByUrl` resolved `wwwroot + relative_path` and then deleted the file without checking that the resolved path stayed inside `wwwroot`. A crafted URL like `/photos/../../../appsettings.json` could delete arbitrary files.

Fix: `Path.GetFullPath` to get the canonicalized absolute path, then compare with `Path.GetFullPath(webRootPath)` using a `StartsWith(root + Path.DirectorySeparatorChar)` check. If the path escapes the root, the deletion is silently skipped.

## Why these choices

- **Relative-only redirect**: Absolute redirect URLs (even `//evil.com`) are blocked; relative paths are safe by definition.
- **Stack-allocated magic bytes**: `Span<byte>` with `stackalloc` avoids a heap allocation for a 4-byte buffer in a hot path.
- **Single-quote prefix for CSV**: Standard OWASP recommendation; doesn't break normal numeric cells.
- **Static DummyHash field**: Computed once at startup (class initializer), not re-hashed per request.
- **StartsWith(root + separator)**: Prevents `root` matching if `wwwroot` is a prefix of another directory name.

## Test suite fixes

After the security changes the test suite required updates:

1. **PaginationTests**: `GetDogs_Page1_AndPage2_HaveNoOverlap` and `GetAdoptions_FilterByStatus_ExcludesOtherStatuses` were seeding data via `anonClient`, which now returns 401 for `POST /api/dogs`. Switched to `IAsyncLifetime` pattern (same as `DogsApiTests`) so a shared `_managerClient` is initialized once before any tests run.

2. **SecurityTests**: Two assertions expected HTTP 302 (redirect to login) for unauthenticated JSON POST requests. ASP.NET Core cookie auth returns 401 for non-HTML API requests. Changed both assertions to accept `401 or redirect` — the security property (blocking unauthenticated access) is still proven.

3. **VolunteerServiceTests**: `Register_WithLogin_EmptyPassword_NoHash` used role `"Helper"` and `Update_UpdatesFields_WhenFound` used role `"Lead"`, both rejected by `ValidateRole`. Changed to `Roles.Volunteer`.

## Alternatives rejected

- **Allowlist redirect to known paths**: More complex and brittle; relative-only is simpler and equally safe.
- **Per-request DummyHash computation**: Wasteful; one static init is enough.
- **File extension allowlist only**: Insufficient — extensions are attacker-controlled. Magic bytes are not.
- **CSV sanitization at query layer**: Wrong layer; sanitize at output (the CSV writer).
