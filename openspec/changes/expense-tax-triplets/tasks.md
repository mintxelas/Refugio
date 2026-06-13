## 1. Domain — ExpenseTaxLine entity

- [x] 1.1 Create `src/Refugio.Domain/Entities/ExpenseTaxLine.cs` extending `Entity` with `ExpenseId`, `IvaPercent`, `Base`, `Importe` and a `Create` factory that validates the math invariant (`round(IvaPercent/100 * Base, 2) == Importe`)
- [x] 1.2 Add `ICollection<ExpenseTaxLine> TaxLines { get; }` to `Expense` and a `SetTaxLines(IEnumerable<ExpenseTaxLine>)` method that enforces 1–5 count
- [x] 1.3 Update `Expense.Record` and `Expense.Update` signatures to accept `IEnumerable<(decimal IvaPercent, decimal Base, decimal Importe)> taxLines` and call `SetTaxLines`

## 2. Infrastructure — EF config and migration

- [x] 2.1 Add `ExpenseTaxLine` `DbSet` and `HasMany` / `WithOne` / `OnDelete(Cascade)` config in `ShelterDbContext`
- [x] 2.2 Run `dotnet ef migrations add AddExpenseTaxLines --project src/Refugio.Infrastructure --startup-project src/Refugio.Web`
- [x] 2.3 Verify migration SQL adds `ExpenseTaxLines` table and apply locally

## 3. Application — contracts, mapping, service

- [x] 3.1 Add `ExpenseTaxLineDto(int Id, decimal IvaPercent, decimal Base, decimal Importe)` to `FinanceContracts.cs`
- [x] 3.2 Add `TaxLineRequest(decimal IvaPercent, decimal Base, decimal Importe)` to `FinanceContracts.cs`
- [x] 3.3 Update `ExpenseDto` to include `List<ExpenseTaxLineDto> TaxLines`
- [x] 3.4 Update `CreateExpenseRequest` and `UpdateExpenseRequest` to include `List<TaxLineRequest> TaxLines`
- [x] 3.5 Update `DtoMapping` to map `ExpenseTaxLine` → `ExpenseTaxLineDto`
- [x] 3.6 Update `FinanceService.RecordExpenseAsync` to pass `taxLines` from request to `Expense.Record`
- [x] 3.7 Update `FinanceService.UpdateExpenseAsync` to replace tax lines: remove existing via repository then call `Expense.SetTaxLines` with new list

## 4. Web — endpoint and React client

- [x] 4.1 Update `FinanceEndpoints` expense create/update route handlers to forward `TaxLines` from request body (no structural change needed if request records already carry them)
- [x] 4.2 Add `ExpenseTaxLineDto` interface to `ClientApp/src/types.ts`; add `taxLines: ExpenseTaxLineDto[]` to `ExpenseDto`
- [x] 4.3 Update `financeApi.createExpense` and `financeApi.updateExpense` signatures in `ClientApp/src/api/finance.ts` to include `taxLines: { ivaPercent: number; base: number; importe: number }[]`

## 5. React UI — inline triplet editor in ExpenseEdit

- [x] 5.1 Add local state `taxLines` (array of `{ivaPercent: string; base: string; importe: string}`) initialised to one empty row for new expenses or mapped from fetched data for existing ones
- [x] 5.2 Render a table with columns IVA %, Base, Importe, and a remove button per row inside the expense `<form>`
- [x] 5.3 Add "Add line" button (disabled when 5 rows) that appends an empty row
- [x] 5.4 Disable the remove button when only 1 row remains
- [x] 5.5 Add per-row inline validation: on submit check `round(ivaPercent/100 * base, 2) == importe` for each row; display error message under offending row
- [x] 5.6 Pass `taxLines` payload in `createExpense` / `updateExpense` calls on form submit

## 6. Tests

- [x] 6.1 Unit test `ExpenseTaxLine.Create` — valid triplet accepted, wrong Importe throws, IvaPercent=0 with Importe=0 accepted
- [x] 6.2 Unit test `Expense.SetTaxLines` — 0 lines throws, 6 lines throws, 1–5 valid
- [x] 6.3 Unit test `FinanceService.RecordExpenseAsync` with tax lines round-trips through in-memory EF
- [x] 6.4 Integration test: POST `/api/expenses` with valid tax lines → 201, GET `/api/expenses/{id}` returns `taxLines`
- [x] 6.5 Integration test: PUT `/api/expenses/{id}` replaces tax lines (starts with 2, updates to 1)
- [x] 6.6 Integration test: POST `/api/expenses` with 0 tax lines → 400
