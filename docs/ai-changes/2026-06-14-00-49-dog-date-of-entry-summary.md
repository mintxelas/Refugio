# Summary: Dog Date of Entry

## What changed

| File | Change |
|---|---|
| `Dog.cs` | `UpdateDetails` accepts `DateTime arrivalDate` and assigns it |
| `DogContracts.cs` | `CreateDogRequest` + `UpdateDogRequest` now include `DateTime ArrivalDate` |
| `DogService.cs` | `CheckInDogAsync` passes `request.ArrivalDate` to `Dog.CheckIn`; `UpdateDogAsync` passes it to `UpdateDetails` |
| `dogs.ts` | `create` + `update` payloads include `arrivalDate: string` |
| `DogCheckin.tsx` | Date of Entry input, pre-filled today, mandatory, validated, submitted |
| `DogEdit.tsx` | Date of Entry input, loaded from DTO, mandatory, validated, submitted |
| `DogDetail.tsx` | Already showed `arrivalDate` — no change |
| `DogServiceTests.cs` | 4 existing call sites updated + 2 new tests |
| Integration tests (7 files) | All dog POST payloads include `ArrivalDate` |
| `ReadQueriesTests.cs` | Pre-existing compile error fixed (`Expense.Record` missing `taxLines`) |

## Why

`ArrivalDate` was silently defaulting to `UtcNow` on every dog created, making it useless for intake tracking and shelter-stay reporting. The field now flows end-to-end: user sets it on check-in, can edit it later, and sees it on the detail page.

## No migration needed

`ArrivalDate` column already existed in the database.

## Side effects / follow-ups

- Any external API client (not the React SPA) that posts `/api/dogs` without `arrivalDate` will receive `DateTime.MinValue` (0001-01-01) as the stored date — a clear signal of missing data rather than a silent wrong date.
- `ReadQueriesTests.cs` fix is a minor housekeeping change to unblock builds for the unrelated ExpenseTaxLines feature work in progress.

## Code review checklist

- `Dog.UpdateDetails` signature includes `arrivalDate` and assigns it
- `CheckInDogAsync` passes named arg `arrivalDate: request.ArrivalDate` (skips optional params correctly)
- React date inputs use `type="date"` (yields `YYYY-MM-DD` strings)
- `DogEdit.tsx` slices the ISO string correctly: `d.arrivalDate.slice(0, 10)`
- Both new unit tests use `DateTimeKind.Utc` to match server behavior
- Integration test payloads include `ArrivalDate = DateTime.UtcNow`
