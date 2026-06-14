# Expense Tax-Line Test Failures — Reasoning

## Goal
Check and fix the failing tests surfaced after the dead-code cleanup.

## Root-cause discipline
Reproduced first, read the spec
(`openspec/changes/expense-tax-triplets/specs/expense-tax-lines/spec.md`) to learn
intended behavior before changing anything. Spec: an expense SHALL have 1–5 tax
lines; create-with-none SHALL return 400; update SHALL replace all lines.

## Three distinct root causes

### 1. Stale test data (test-only)
Pre-feature tests created expenses without tax lines. The new domain invariant
(`Expense.SetTaxLines`, 1–5 required) correctly rejected them. Fix: supply valid
`taxLines` triplets in the affected helpers/seeds. Triplet math invariant is
`round(IvaPercent/100 × Base, 2) == Importe`; used zero-rate `(0, base, 0)` where the
value is irrelevant and `(21, 250.00, 52.50)` for the CSV test.

### 2. `ExpenseTaxLine` had no soft-delete global query filter (production bug)
`UpdateExpense` replaces lines via `SetTaxLines` → `TaxLines.Clear()`. The
`SoftDeleteInterceptor` converts the severed children into soft-deletes
(`DeletedAt` set) rather than hard-deleting them. But `ExpenseTaxLine` was missing
from the `HasQueryFilter(e => e.DeletedAt == null)` block in `ShelterDbContext`,
so the `Include(e => e.TaxLines)` on read pulled the soft-deleted rows back in —
`UpdateExpense_ReplacesTaxLines` saw 3 lines instead of 1. Every other child entity
(`DogPhoto`, `ExpensePhoto`, …) already had the filter; this was a plain omission.
Fix: add the filter for `ExpenseTaxLine`.

### 3. Domain `ArgumentException` returned 500, not 400 (production bug)
`CreateExpense_WithNoTaxLines_Returns400` expected 400. The domain throws
`ArgumentException`; the actor wraps it in `Status.Failure`, Akka faults the `Ask`
with the original exception, and the endpoint had no mapping → unhandled → 500.
Fix: a minimal `app.UseExceptionHandler` in `Program.cs` mapping `ArgumentException`
→ 400 (with the message) and everything else → 500. Centralized, so create/update
and any future domain validation get the same treatment.

## Why these choices
- **Query filter over hard-delete on replace**: matches the established pattern for
  every other child entity; no special-casing in the domain.
- **Global exception handler over per-endpoint try/catch**: one place, covers all
  domain validation, keeps endpoints thin.

## Alternatives rejected
- Relaxing the 1–5 invariant to make tests pass — rejected: contradicts the spec.
- Hard-deleting orphan tax lines in the repository — rejected: inconsistent with the
  soft-delete model used everywhere else.

## Verification
- No EF migration needed (query filters are model-only, not schema):
  `dotnet ef migrations has-pending-model-changes` → "No changes".
- Unit 245/245, Integration 144/144 green.
