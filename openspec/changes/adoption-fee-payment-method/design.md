## Context

The `adoption-dates-and-fees` change (just implemented) added `PreAdoptionFeeCharged` and `AdoptionFeeCharged` boolean fields to the `Adoption` aggregate. This change adds a companion payment-method field per fee stage so staff can record how the fee was paid.

The project stores enums as strings (`HasConversion<string>()`) — member names are permanent identifiers. The UI is a React SPA; there is no localization system (strings are hardcoded in JSX). Spanish display labels are hardcoded in the form component.

## Goals / Non-Goals

**Goals:**
- Add `FeePaymentMethod` C# enum (`Cash`, `Bizum`, `Transfer`) to the `Adoption` entity namespace
- Add `PreAdoptionFeePaymentMethod` (`FeePaymentMethod?`) and `AdoptionFeePaymentMethod` (`FeePaymentMethod?`) properties
- Wire through contracts, mapping, service, and migration
- Render as dropdowns in `AdoptionEdit.tsx` with Spanish labels ("Metálico", "Bizum", "Transferencia")
- Fields are nullable: a fee may be charged without recording the method (or no fee at all)

**Non-Goals:**
- No validation enforcing that payment method is set when fee-charged is true — optional fields only
- No reporting or aggregation by payment method in this change
- No change to `ChangeStatus` or domain events

## Decisions

**D1 — Enum, not free-text string**
`FeePaymentMethod` is an enum stored as a string. Values are fixed (`Cash`, `Bizum`, `Transfer`); an enum prevents invalid entries and makes the API contract explicit. Alternative (free-text) rejected: no constraint on values, harder to query/report.

**D2 — Nullable, independent of the fee-charged flag**
`PreAdoptionFeePaymentMethod` is `null` when no fee was charged OR when the method wasn't recorded. No cross-field validation in the domain — staff may fill them independently. Alternative (non-nullable with a `None` member) rejected: adds noise to all existing records.

**D3 — Enum defined in `Adoption.cs`**
Co-located with `AdoptionType` and `AdoptionStatus` in `Adoption.cs` (same file), keeping adoption-specific enums together. The CLAUDE.md convention is namespace `Refugio.Domain.Entities` — respected.

**D4 — Spanish labels in the React component**
The project has no localization (CLAUDE.md: "UI strings are hardcoded in English in JSX"). This change hardcodes Spanish labels directly in `AdoptionEdit.tsx` because the shelter operates in Spanish. A lookup map (`{ Cash: 'Metálico', Bizum: 'Bizum', Transfer: 'Transferencia' }`) is defined in the component.

**D5 — EF migration stores enum as string**
`HasConversion<string>()` is the project convention for all enums. Member names (`Cash`, `Bizum`, `Transfer`) are permanent identifiers. Two nullable TEXT columns added.

## Risks / Trade-offs

- **DTO breaking change**: `AdoptionDto` gains two new nullable fields — existing API consumers (React `types.ts`) must be updated simultaneously (project-wide convention, not new risk).
- **Nullable enum JSON serialization**: nullable enum + `JsonStringEnumConverter` → `null` or the member name string. Ensure `types.ts` uses `FeePaymentMethod | null`.

## Migration Plan

1. Add enum + properties to entity
2. Update `Submit` / `UpdateDetails`
3. Update contracts + mapping + service
4. `dotnet ef migrations add AdoptionFeePaymentMethod`
5. Update React `types.ts` + `api/adoptions.ts` + `AdoptionEdit.tsx`
6. Run unit + integration tests
