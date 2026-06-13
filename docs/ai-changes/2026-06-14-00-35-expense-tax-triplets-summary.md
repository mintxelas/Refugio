# Summary: Expense IVA Tax Triplets

## What changed

| File | Change |
|---|---|
| `Domain/Entities/ExpenseTaxLine.cs` | New child entity with `Create` factory (math invariant validation) |
| `Domain/Entities/Expense.cs` | Added `TaxLines` collection, `SetTaxLines` method, updated `Record`/`Update` signatures |
| `Infrastructure/Data/ShelterDbContext.cs` | `ExpenseTaxLines` DbSet + HasMany/cascade/decimal column config |
| `Infrastructure/Repositories/FinanceRepositories.cs` | Override `GetAsync` to include `TaxLines`; include in `GetAllAsync` |
| `Infrastructure/Data/SeedData.cs` | Seed expenses updated with tax lines |
| `Infrastructure/Migrations/*_AddExpenseTaxLines.cs` | New migration |
| `Application/Contracts/FinanceContracts.cs` | `ExpenseTaxLineDto`, `TaxLineRequest`, updated `ExpenseDto`/request records |
| `Application/Mapping/DtoMapping.cs` | `ExpenseTaxLine.ToDto()`, updated `Expense.ToDto()` |
| `Application/Services/FinanceService.cs` | Pass tax lines through `RecordExpenseAsync`/`UpdateExpenseAsync` |
| `Web/ClientApp/src/types.ts` | `ExpenseTaxLineDto` interface, `taxLines` on `ExpenseDto` |
| `Web/ClientApp/src/api/finance.ts` | `taxLines` in `createExpense`/`updateExpense` bodies |
| `Web/ClientApp/src/pages/ExpenseEdit.tsx` | Inline tax line table with add/remove/validate |
| `Tests.Unit/Domain/ExpenseTaxLineTests.cs` | Domain unit tests (8 cases) |
| `Tests.Unit/Services/FinanceServiceTests.cs` | Fixed `SeedExpense`, updated existing tests, added round-trip test |
| `Tests.Integration/ExpenseTaxLineTests.cs` | 3 integration tests (create, replace, 0-lines → 400) |

## Why
Spanish fiscal compliance: VAT breakdowns on expenses.

## Side effects / follow-up
- Existing expenses in production DB have no tax lines. The constraint is API-only — the DB does not enforce it. Existing rows won't break but can't be updated via the API without providing at least one tax line.
- The `FinanceCsvTests` export tests continue to pass (no changes to export logic).

## Code review checklist
- [ ] `SetTaxLines` mutates in-place (not replacing the list reference) — required for EF tracking
- [ ] `ExpenseRepository.GetAsync` includes `TaxLines` — required for update replace-all to work
- [ ] Math rounding: `Math.Round(iva / 100m * base, 2)` — matches `Math.round` in React's `validateRow`
- [ ] `TaxLines` initialized to `[]` in `Expense` (not null) — `Clear()` won't throw on new entities
- [ ] All existing unit + integration tests pass (225 + 135)
