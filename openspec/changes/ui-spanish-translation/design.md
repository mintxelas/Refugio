## Context

The React SPA has ~23 page files, 7 component files, and an auth context. All user-visible strings are hardcoded English in JSX — no i18n library, no translation keys, no resource files. Enum values from the API (e.g., "Applied", "Available") are rendered raw in `StatusChip` and various dropdowns/selects without display mapping. The project convention explicitly forbids a localization framework ("strings are hardcoded in JSX").

C# enum member names (`AdoptionStatus.Applied`, `DogStatus.Available`, etc.) are permanent DB/API identifiers stored as strings via `HasConversion<string>()`. They cannot be renamed without a data migration on all existing rows.

## Goals / Non-Goals

**Goals:**
- All user-visible text in the React SPA is Spanish
- Enum values rendered in the UI show Spanish labels (not raw member names)
- One shared label-map file centralizes all enum display translations
- C# backend unchanged

**Non-Goals:**
- No i18n library or translation key system
- No English language support or language toggle
- No C# enum renames, no DB migration
- No backend API changes

## Decisions

**D1 — Shared `labels.ts` for all enum display maps**
Create `src/Refugio.Web/ClientApp/src/labels.ts` with one `Record<EnumType, string>` per enum. All pages and components import from here. Alternative (inline maps per component) rejected: duplication, inconsistent translations, harder to audit.

**D2 — `StatusChip` renders label not raw value**
`StatusChip` imports the relevant label map and renders `labelMap[status]` instead of `{status}`. The chip color logic stays keyed on the enum member name (unchanged). Alternative (pass label as prop) rejected: callers would each need to import the label map anyway.

**D3 — FeePaymentMethod labels already exist in `AdoptionEdit.tsx` — move to `labels.ts`**
`PAYMENT_METHOD_LABELS` defined in `AdoptionEdit.tsx` gets moved to `labels.ts` and imported. Keeps one source of truth.

**D4 — Page-by-page translation, no string extraction**
Strings stay inline in JSX — they just change from English to Spanish. No key-value indirection. Consistent with the project convention and avoids adding abstraction for a single-language app.

**D5 — Confirmation dialogs (`confirm(...)`) also translated**
Native `window.confirm` strings in delete/purge handlers are Spanish too.

## Risks / Trade-offs

- **Volume**: 23 pages × many strings = large diff. Risk of missed strings. Mitigation: grep for common English words (`Save`, `Cancel`, `Delete`, `Error`, `Loading`) after implementation to catch stragglers.
- **Enum label map completeness**: If a new enum member is added later, the label map must be updated simultaneously or TypeScript will error (using `Record<EnumType, string>` enforces exhaustiveness). This is a feature, not a bug.
- **`confirm()` strings**: Browser native dialogs ignore the app's styling. Acceptable — no modal replacement in scope.

## Migration Plan

1. Create `labels.ts` with all enum label maps
2. Update `StatusChip` to use label maps
3. Translate shared components (Sidebar, TopBar, Pagination, ErrorMessage)
4. Translate each page file
5. Move `PAYMENT_METHOD_LABELS` from `AdoptionEdit.tsx` to `labels.ts`
6. Verify no raw English enum values render in the UI (grep check)
