# Reasoning: ui-spanish-translation

## Problem
All React SPA user-visible text was in English. The shelter operates in Spanish and required a full translation with no English fallback and no i18n library.

## Approach

### Step 1 — Central enum label maps (`labels.ts`)
C# enum member names (`Available`, `Applied`, `Cash`) are permanent API/DB identifiers and cannot change. A new `labels.ts` file was created with `Record<EnumType, string>` maps for all 7 enum types. Using `Record<T, string>` means TypeScript enforces exhaustiveness — adding a new enum member without updating the map causes a build error.

### Step 2 — StatusChip.tsx
Updated to import from `labels.ts` and render the Spanish label. Color logic remains keyed on the enum member name (unchanged).

### Step 3 — Shared components
Sidebar, TopBar, Pagination — simple string replacements. No logic changes.

### Step 4–7 — Page files (33 total)
Each page translated in place: labels, placeholders, error messages, confirm dialogs, button text, empty states. Enum dropdowns use the label maps so the `value` attribute (the API string) stays in English while `children` is Spanish.

### Gender options
`<option value="Male">Macho</option>` — value preserved for API; display is Spanish.

### Locale strings
`toLocaleDateString('es-ES', ...)` and `toLocaleString('es-ES', ...)` used throughout for dates and month names.

## Alternatives considered
- i18n library (react-i18next): rejected per project convention (strings hardcoded; no localization infrastructure).
- Enum display via switch/if: rejected in favor of `Record<T, string>` maps for compile-time exhaustiveness.
