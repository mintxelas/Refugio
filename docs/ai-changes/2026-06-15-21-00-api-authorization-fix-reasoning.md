# Reasoning: API Authorization Fix

## Problem identified

A security audit revealed three critical vulnerabilities:

1. **CRIT-1 — Privilege escalation via `POST /api/volunteers`**: Endpoint was anonymous, accepted `Role` as a caller-supplied field with no validation, so any anonymous caller could register a Manager account and take full control.
2. **CRIT-2 — Broken access control across the entire API**: ~40 JSON CRUD endpoints had no `RequireAuthorization`. Anonymous callers could read PII (donor names, tax IDs, applicant contacts) and mutate any record.
3. **HIGH-3 — Mass assignment on volunteer update**: `PUT /api/volunteers/{id}` was also anonymous and accepted `Role`.

## Fix strategy

### Step 1 — Domain: validate role in `Volunteer.Register` and `Volunteer.Update`

Added `ValidateRole(role)` as a private static method throwing `ArgumentException` when role is not one of the two canonical values (`Manager`, `Volunteer`). This is a defense-in-depth layer so the domain itself rejects invalid role values even if a future endpoint bypasses the API-level guard.

### Step 2 — Default-deny on the API group

Changed `app.MapGroup("/api")` to `app.MapGroup("/api").RequireAuthorization()`. This makes every endpoint in the group require authentication by default, reversing the prior opt-in approach that already missed ~40 endpoints.

### Step 3 — Explicit AllowAnonymous for public reads

The shelter's public-facing dog catalog and the dashboard stats should remain accessible without login. Added `.AllowAnonymous()` to:
- `GET /api/dashboard`
- `GET /api/dogs`, `GET /api/dogs/paged`, `GET /api/dogs/{id}`, `GET /api/dogs/{id}/photos`
- `POST /api/auth/login` and `POST /api/auth/logout` (bootstrap requirement)

### Step 4 — Manager-only on volunteer management

Added `.RequireAuthorization("Manager")` to `POST /api/volunteers`, `PUT /api/volunteers/{id}`, `PUT /api/volunteers/{id}/status`, `DELETE /api/volunteers/{id}`. Only a Manager can create or modify volunteer accounts (the primary escalation vector).

### Step 5 — Tests

Wrote `SecurityTests.cs` asserting the secure behavior (401/redirect for anon on protected endpoints, 200 for truly public endpoints). Updated 8 existing test files whose data-setup helpers used anonymous clients for write operations — changed to use the Manager-authenticated client.

## Alternatives considered

- **Per-endpoint opt-in**: Rejected. The existing code proved this model fails (missed 40 endpoints already). Default-deny is the only safe baseline.
- **Making all reads auth-gated**: Keeping dogs/dashboard anonymous was intentional — a shelter website reasonably shows available dogs publicly without login. All financial, medical, and PII data is auth-gated.
- **Role validation in the service layer only**: Rejected in favor of domain-layer validation as the single source of truth. The service layer passes role through from the contract; without domain validation, a future refactor bypassing the service would bypass the check.
