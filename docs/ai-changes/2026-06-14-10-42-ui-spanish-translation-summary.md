# Summary: ui-spanish-translation

## What changed
- Created `ClientApp/src/labels.ts` — exhaustive `Record<EnumType, string>` maps for all 7 enums.
- Updated `StatusChip.tsx` — renders Spanish labels from `labels.ts`.
- Translated all 3 shared components: `Sidebar.tsx`, `TopBar.tsx`, `Pagination.tsx`.
- Translated all 28 page files across auth, dogs, adoptions, finance, calendar, volunteers, admin, home.
- Replaced `'en-US'` locale with `'es-ES'` in all `toLocaleDateString` / `toLocaleString` calls.
- All `confirm()` dialogs translated to Spanish.

## Why
Full Spanish-only UI. No i18n library per project convention. Enum member names unchanged — only display labels changed.

## Side effects / follow-up
- No C# changes; the API contract is unchanged.
- If a new enum member is added in C#, TypeScript will fail to build until `labels.ts` is updated (by design).
- The `preferredLanguage` field on volunteers still accepts any string (e.g. `es-ES`) — no functional change.

## Code review checklist
- [ ] All `option value=` attributes in dropdowns still use the C# enum member name (not the Spanish label).
- [ ] `labels.ts` maps cover all members of each enum type (TypeScript will catch missing ones at build time).
- [ ] `toLocaleDateString('es-ES', ...)` used consistently — no stray `'en-US'` calls.
- [ ] No English strings remain in visible JSX text nodes (grep verified clean).
- [ ] `npm run build` passes with zero TypeScript errors.
