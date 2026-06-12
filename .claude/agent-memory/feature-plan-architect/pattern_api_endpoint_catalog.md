---
name: pattern-api-endpoint-catalog
description: ShelterApiClient.cs is the authoritative catalog of every /api/* endpoint and DTO shape
metadata:
  type: reference
---

`src/Refugio.Web/Services/ShelterApiClient.cs` is the single best map of the entire REST surface — it
lists every `/api/*` route, its query-string params, and its request/response DTO type in one file.
Read it first when a plan needs to know what endpoints exist or what shape they return.

Companion facts:
- DTO records live in `src/Refugio.Application/Contracts/*.cs` (DogContracts, AdoptionContracts,
  VolunteerContracts, FinanceContracts, TaskAndEventContracts, SettingsContracts, StatsContracts).
- Endpoint registration + RBAC live in `src/Refugio.Web/Endpoints/*.cs` (Dog, Adoption, Finance,
  Volunteer, Task, Settings, Auth). Manager-only routes use `.RequireAuthorization("Manager")`.
- Soft-delete pattern: each entity has `GET /api/{x}/deleted`, `POST /{id}/delete`, `/{id}/restore`,
  `/{id}/purge`. Browser mutations are POST (not DELETE verb); REST keeps DELETE for external use.
- File uploads (dog photo, expense receipt, settings logo) are multipart POST and currently respond
  with `Results.Redirect(...)` (SSR-oriented) — a JSON consumer needs new JSON-returning variants.
- CSV export: `GET /api/export/{donations|expenses|adoptions}` in FinanceEndpoints.cs.
- Auth: form endpoints `/auth/login` (POST→redirect), `/auth/logout` (GET→redirect),
  `/auth/change-password` (POST→redirect) in AuthEndpoints.cs. Cookie auth, 7-day sliding.
