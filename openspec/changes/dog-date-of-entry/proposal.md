## Why

The dog check-in and edit forms omit the "Date of Entry" (`ArrivalDate`) field, so it is silently set to `UtcNow` on create and never editable — making the field unreliable for reporting shelter-stay duration and intake tracking.

## What Changes

- Add `ArrivalDate` (mandatory, date-only input) to `CreateDogRequest` and wire it through `Dog.CheckIn`.
- Add `ArrivalDate` to `UpdateDogRequest` and wire it through `Dog.UpdateDetails`.
- Expose the field in `DogCheckin.tsx` (check-in form) and `DogEdit.tsx` (edit form).
- Display `ArrivalDate` in `DogDetail.tsx` (currently absent from the detail view).

## Capabilities

### New Capabilities

- `dog-date-of-entry`: User-editable Date of Entry field on dog check-in form, edit form, and detail view — mandatory, stored as `ArrivalDate` on the `Dog` entity.

### Modified Capabilities

*(none — `ArrivalDate` already exists on the entity and the DTO; no schema migration required)*

## Impact

- `Refugio.Application/Contracts/DogContracts.cs` — `CreateDogRequest`, `UpdateDogRequest`
- `Refugio.Domain/Entities/Dog.cs` — `UpdateDetails` signature
- `Refugio.Application/Services/DogService.cs` — `CheckInDogAsync`, `UpdateDogAsync`
- `Refugio.Web/ClientApp/src/api/dogs.ts` — `create` and `update` payload types
- `Refugio.Web/ClientApp/src/pages/DogCheckin.tsx` — add date input, validate, submit
- `Refugio.Web/ClientApp/src/pages/DogEdit.tsx` — add date input, load from DTO, submit
- `Refugio.Web/ClientApp/src/pages/DogDetail.tsx` — display `arrivalDate`
- Unit tests: `CheckInDogAsync`, `UpdateDogAsync` — assert `ArrivalDate` round-trips correctly
