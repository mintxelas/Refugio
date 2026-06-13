## Context

`Dog.ArrivalDate` (DateTime) already exists on the entity and is persisted in the database — no migration required. The field defaults to `UtcNow` in `Dog.CheckIn` and is serialized in `DogDto.ArrivalDate`. However it is never accepted from the caller: `CreateDogRequest` and `UpdateDogRequest` omit it, `Dog.UpdateDetails` ignores it, and neither the check-in nor edit forms expose it.

## Goals / Non-Goals

**Goals:**
- Accept `ArrivalDate` from the API on both create and update paths.
- Validate it as mandatory (non-null) in both request records.
- Render a date input in `DogCheckin.tsx` and `DogEdit.tsx`, pre-filled with today / the existing value.
- Display `arrivalDate` in `DogDetail.tsx`.

**Non-Goals:**
- No database migration (column already exists).
- No change to the REST endpoint URLs or HTTP verbs.
- No time-of-day precision — store and display date only (ISO date string `YYYY-MM-DD` from the React form, parsed server-side as UTC midnight).

## Decisions

### Date-only input, UTC midnight storage
The React date `<input type="date">` yields `"YYYY-MM-DD"`. The C# model binder parses this as `DateTime` (midnight, unspecified kind). Store as UTC midnight via `DateTime.SpecifyKind(value, DateTimeKind.Utc)` in the service layer (or accept the model-binder default — SQLite stores as text anyway). This keeps it consistent with how `MedicalRecord.VisitDate` works elsewhere.

**Alternative considered:** Store as `DateOnly`. Rejected — EF SQLite provider and existing entity base use `DateTime`; changing type would require a migration and snapshot churn.

### `UpdateDetails` signature extended
Add `DateTime arrivalDate` parameter to `Dog.UpdateDetails`. This stays consistent with the existing pattern where the method accepts the full editable set of fields.

**Alternative considered:** Separate `SetArrivalDate` method. Rejected — overkill for a single scalar; the existing `UpdateDetails` call-site already passes all fields in one go.

### Mandatory on API — no server-side default fallback
`CreateDogRequest.ArrivalDate` is non-nullable `DateTime`. The client always sends it (defaulting to today). Removing the server-side fallback `?? DateTime.UtcNow` makes intent explicit and prevents silent wrong dates.

## Risks / Trade-offs

- Existing API consumers (if any) that omit `ArrivalDate` from the create payload will get a 400 / model-binding error → Mitigation: only the React SPA calls this endpoint; the SPA is updated in the same change.
- Tests that call `CreateDogRequest` without `ArrivalDate` will fail to compile → Mitigation: update affected unit/integration tests in the same change.

## Migration Plan

1. Update domain entity `UpdateDetails` signature.
2. Update `CreateDogRequest` / `UpdateDogRequest` contracts.
3. Update service methods (`CheckInDogAsync`, `UpdateDogAsync`).
4. Update React forms (`DogCheckin.tsx`, `DogEdit.tsx`, `DogDetail.tsx`) and `dogs.ts` API client.
5. Update unit tests.
6. No migration step — column already in schema.
