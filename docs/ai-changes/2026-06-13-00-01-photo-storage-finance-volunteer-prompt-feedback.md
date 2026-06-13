# Prompt feedback — Photo storage reorganization: finance (expenses) and volunteer

## Original prompt

> same for adoption (photos/adoption/{id}/ folder), finance (photos/finance/{finance_type}/{id}/ folder), and volunteer (photos/volunteer/{id} folder) pictures.

## What worked well

- `{finance_type}` placeholder is good: signals the path must carry the entity subtype (expenses, donations, goals) not just "finance".
- Per-entity subfolder convention is unambiguous.
- Referencing the previous change as "same" kept the diff instructions short and implied identical constraints (ext, size).

## Issues

**Adoption has no photos.** The `Adoption` aggregate has no `PhotoUrl`, no child photo entity, and no upload endpoint. There is nothing to migrate. The prompt assumed adoption has pictures, but it does not. Two possible intents:
1. Establish the path convention for future adoption photos (no code change now).
2. Implement adoption photo support from scratch (new entity, migration, service, endpoint, UI).

Neither was done — the prompt requires clarification.

**Finance type was under-specified.** Only `Expense` has photos (`ExpensePhoto`). `Donation` and `Goal` have no photos. The path `photos/finance/{finance_type}/` implies multiple subtypes exist; only `expenses` is live. If donations or goals ever get photos, the convention already accommodates them.

## Suggested improved prompt

> Apply the same photo storage convention (per-entity subfolder, `.jpg/.png` only, 2 MB max) to:
> - **Expense receipt photos**: `wwwroot/photos/finance/expenses/{expenseId}/` (Donations and Goals have no photos — skip).
> - **Volunteer profile photo**: `wwwroot/photos/volunteer/{volunteerId}/primary.{ext}` (single-replacement, same as dog primary photo).
> - **Adoption**: Currently no photo infrastructure exists. Do NOT implement it now — just note the intended path convention (`photos/adoption/{adoptionId}/`) in the feature doc for future reference.
> Leave existing files in place; update feature docs accordingly.
