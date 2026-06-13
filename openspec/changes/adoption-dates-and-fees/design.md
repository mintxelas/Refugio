## Context

`Adoption` is a DDD aggregate root in `Refugio.Domain/Entities/Adoption.cs`. It currently tracks application timestamps (`CreatedAt`, `UpdatedAt`) but not the shelter's operational milestones: the pre-adoption hand-off date, the formal adoption date, and whether a fee was collected at each stage.

The feature crosses five layers: Domain entity → Application contracts/service/mapping → Infrastructure migration → Web API endpoint → React SPA form/view.

## Goals / Non-Goals

**Goals:**
- Add `PreAdoptionDate`, `AdoptionDate` (nullable `DateTime`), `PreAdoptionFeeCharged`, `AdoptionFeeCharged` (non-nullable `bool`, default `false`) to `Adoption`
- Thread all four fields through `Submit`, `UpdateDetails`, contracts, mapping, and the React form
- EF Core migration adds four nullable columns (dates) and two bit columns (fees, with DB default `0`)
- All four fields editable in `AdoptionEdit.tsx` and visible wherever adoption details are rendered

**Non-Goals:**
- No business rules enforcing date ordering (e.g. pre-adoption before adoption) — shelter staff decide
- No fee amount tracking — only a boolean flag ("was a fee charged?")
- No reporting or aggregation on fees/dates in this change

## Decisions

**D1 — Nullable dates, non-nullable booleans**
`PreAdoptionDate` and `AdoptionDate` are nullable: many adoptions will not have these dates set yet. Fee flags default to `false` (not charged); making them non-nullable avoids three-state ambiguity.

**D2 — Both dates passed through `Submit` and `UpdateDetails`**
`Submit` (create path) and `UpdateDetails` (edit path) both accept all four parameters. This keeps the factory and the mutator in sync and avoids partial-update patterns. Parameters are optional with sensible defaults (`null` / `false`).

**D3 — No domain event for date/fee changes**
Date and fee fields are administrative bookkeeping. They do not change the adoption's workflow status, so no `AdoptionStatusChanged` event is raised (mirroring the existing `UpdateDetails` precedent).

**D4 — React date inputs use `<input type="date">`**
ISO 8601 (`yyyy-MM-dd`) is sent to the API as a date-only string; the backend parses it as `DateTime` (UTC midnight). No time component is captured or displayed.

## Risks / Trade-offs

- **EF migration on existing rows**: New bool columns get DB default `0` (false); date columns are nullable so existing rows get `NULL` — no data loss, no backfill needed.
- **Two-place DTO update**: Any C# contract change requires a matching `types.ts` update (project-wide convention — not new risk).

## Migration Plan

1. Add properties to `Adoption` entity
2. Update `Submit` / `UpdateDetails` signatures
3. Update contracts + mapping + service
4. `dotnet ef migrations add AdoptionDatesAndFees`
5. Apply migration (auto-applies on startup)
6. Update React `types.ts` + `api/adoptions.ts` + `AdoptionEdit.tsx`
7. Run integration + unit tests
