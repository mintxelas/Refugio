# Expense Tax-Line Test Failures — Summary

## What changed
| File | Change | Type |
|---|---|---|
| `ShelterDbContext.cs` | Added `ExpenseTaxLine` soft-delete global query filter | **Production fix** |
| `Program.cs` | `UseExceptionHandler` maps `ArgumentException` → 400 | **Production fix** |
| `RestoreApiTests.cs`, `PurgeApiTests.cs`, `DeleteEndpointTests.cs` | `CreateExpenseAsync` helper now sends a valid tax line | Test data |
| `FinanceCsvTests.cs` | Expense POST sends a valid tax line | Test data |
| `ReadQueriesTests.cs` | Two seeded expenses now carry a tax line | Test data |

## Why
The `ExpenseTaxLines` feature (commit `e15ad83`) shipped with two latent production
defects and a batch of pre-feature tests that no longer satisfied the new
1–5-tax-lines invariant. Detected when the dead-code cleanup ran the full suite.

## Defects fixed (not just tests)
1. **Soft-deleted tax lines leaked into reads** — `ExpenseTaxLine` was missing its
   global query filter, so editing an expense's tax lines accumulated ghost rows in
   `GET /api/expenses/{id}`.
2. **Validation returned HTTP 500** — domain `ArgumentException` (e.g. 0 or >5 tax
   lines) was unmapped; now returns 400 with `{ "error": "<message>" }`.

## Side effects / follow-ups
- The 400 mapping is global: **any** `ArgumentException` thrown from a request now
  becomes a 400. That is the desired contract for domain validation here; be aware if
  some future code throws `ArgumentException` for a non-client-error reason.
- Editing tax lines leaves soft-deleted `ExpenseTaxLine` rows in the table (same as
  photos). They are filtered from all normal reads. Purge if cleanup is ever needed.
- No migration: query filters are model-only. Confirmed via
  `has-pending-model-changes` → "No changes".

## Code-review checklist
1. `ShelterDbContext` — confirm the new filter sits with the other child-entity
   filters and matches their shape.
2. `Program.cs` — confirm `UseExceptionHandler` is early in the pipeline (before
   auth/endpoints) and the non-`ArgumentException` branch still yields 500.
3. Confirm the new test tax lines satisfy `round(IvaPercent/100 × Base,2)==Importe`.
4. Re-run `dotnet test` — Unit 245/245, Integration 144/144.
