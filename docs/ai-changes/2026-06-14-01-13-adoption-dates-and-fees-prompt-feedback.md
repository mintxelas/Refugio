# Prompt Feedback: Adoption Dates and Fees

## Original Prompt
> Ensure the adoption entity has a "Pre-Adoption Date" and "Adoption Date" fields. Add also a Pre-Adoption Fee (boolean) and Adoption Fee (boolean) fields. Make them 4 available in all Adoption forms and views.

## What Worked
- Clear field names and types (boolean flags explicitly stated).
- "All forms and views" gave a clear scope.

## What Could Be Improved

### 1. Clarify "views"
The prompt says "all Adoption forms and views" but the codebase only has `AdoptionEdit.tsx` (edit form). The list page (`Adoptions.tsx`) and detail view (`AdoptionDetail.tsx` — if it existed) are also "views." Specify which views should DISPLAY the fields (read-only) vs which should allow editing.

**Improved**: "Add the four fields to the `AdoptionEdit` form for editing. Also display them read-only in the adoption list cards and/or a detail view if one exists."

### 2. Specify date semantics
"Pre-Adoption Date" and "Adoption Date" — are these date-only or datetime? The current implementation uses `DateTime?` (UTC midnight) for consistency with the rest of the codebase, but a date-only interpretation might be intended.

**Improved**: "These dates are calendar-date-only (no time), entered via a date picker. Store as `DateTime?` (UTC midnight) since the project uses `DateTime` throughout."

### 3. Clarify "all forms" scope
Does "all forms" include the adoption creation flow (new adoption)? The current `AdoptionEdit.tsx` covers editing, but there may be a separate create form or the same form is reused. Clarifying prevents ambiguity.

**Improved**: "The edit form at `/adoptions/:id` should include all four fields. The create flow (if separate) should also accept them."

### 4. Mention test expectations
The prompt doesn't say anything about tests. Since the project convention requires tests for every change, specifying "include unit and integration tests for round-trip" helps scope the work explicitly.

**Improved version of the full prompt**:
> Add four fields to the `Adoption` aggregate: `PreAdoptionDate` (`DateTime?`, date-only input), `AdoptionDate` (`DateTime?`, date-only input), `PreAdoptionFeeCharged` (`bool`, default false), `AdoptionFeeCharged` (`bool`, default false). Wire them through the contracts, service, EF migration, and React `AdoptionEdit.tsx` form. Display them read-only wherever adoption details are shown. Add unit and integration tests covering create and update round-trips.
