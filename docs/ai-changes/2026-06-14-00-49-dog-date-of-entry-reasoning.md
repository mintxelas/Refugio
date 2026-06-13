# Reasoning: Dog Date of Entry

## Problem analysis

`Dog.ArrivalDate` (DateTime) already existed on the entity and was serialized in `DogDto`. Gap: it was never accepted from callers. `CheckIn` accepted `DateTime? arrivalDate = null` and defaulted to `UtcNow`; `UpdateDetails` had no `arrivalDate` parameter at all. Neither `CreateDogRequest` nor `UpdateDogRequest` carried the field. Both forms (DogCheckin, DogEdit) had no date input.

## Steps taken

1. **Domain** — Added `DateTime arrivalDate` to `Dog.UpdateDetails` signature and assigned it. No change to `CheckIn` factory (already accepted optional `arrivalDate`; just needed the service to pass it).

2. **Contracts** — Appended `DateTime ArrivalDate` to `CreateDogRequest` and `UpdateDogRequest` positional records. Put it last to minimize positional-argument impact on existing callers.

3. **Service** — `CheckInDogAsync`: passed `arrivalDate: request.ArrivalDate` (named arg to skip optional params). `UpdateDogAsync`: passed `request.ArrivalDate` as the new last arg to `UpdateDetails`.

4. **React API client** — Added `arrivalDate: string` to both `create` and `update` payload types in `dogs.ts`.

5. **DogCheckin.tsx** — Computed `today` once at render time. Added to form state with default = today. Added validation rule. Added `<input type="date">` above Traits field. Submitted in payload. Added `max={today}` to avoid future dates.

6. **DogEdit.tsx** — Same pattern. On load: `d.arrivalDate.slice(0, 10)` extracts `YYYY-MM-DD` from the ISO string. Added validation. Added input field. Submitted in payload.

7. **DogDetail.tsx** — Already rendered `arrivalDate` on line 78 via `new Date(dog.arrivalDate).toLocaleDateString()`. No change needed.

8. **Tests** — Updated 4 existing unit-test call sites and 8 integration-test dog POST bodies. Added 2 new unit tests (`CheckInDog_PersistsArrivalDate`, `UpdateDog_UpdatesArrivalDate`).

9. **Pre-existing fix** — `ReadQueriesTests.cs` had a compilation error (Expense.Record signature changed by ExpenseTaxLines feature without updating the test). Fixed by passing `[]` as the `taxLines` argument in 2 calls. This unblocked unit test execution.

## Alternatives rejected

- **`DateOnly` type**: would require a migration and snapshot churn; `DateTime` is consistent with `MedicalRecord.VisitDate`.
- **Separate `SetArrivalDate` method on Dog**: unnecessary for a single scalar; `UpdateDetails` already takes the full editable set.
- **Server-side default fallback on missing ArrivalDate**: removed; client always sends it (defaulted to today), making intent explicit.
