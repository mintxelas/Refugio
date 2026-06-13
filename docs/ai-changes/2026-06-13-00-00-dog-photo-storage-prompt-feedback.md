# Prompt feedback — Dog photo storage reorganization

## Original prompt

> add feature: all dog pictures go to a folder named photos/dogs/{dog_id}/ and the their path is stored in the database, in order to load them from there when they are displayed. Only PNG and JPG extensions are allowed. Files can have be maximum of 2MB in size each. Update both the front and backend to implement the feature. Add a .md entry in the features folder following the style of the other features.

## What worked well

- Clear path convention (`photos/dogs/{dog_id}/`) — zero ambiguity.
- Explicit extension list (PNG and JPG) — no guessing.
- Explicit size constraint (2 MB) — no guessing.
- "Update both front and backend" flags that UI-facing behavior must also change.
- Reference to the features folder style ensures documentation format is correct.

## What could be improved

1. **Clarify "frontend" scope.** "Update the frontend" is ambiguous — does it mean the Blazor pages, the React SPA, or both? In this case the upload logic lives entirely in the API endpoints (backend), and the Blazor pages use `<form>` posts with no client-side file handling. Stating "the validation lives in the API endpoint, not in JavaScript" would save investigation time.

2. **Migration strategy for existing photos.** The prompt doesn't say what to do with photos already stored under `wwwroot/dogs/`. An explicit "leave existing photos as-is" or "migrate existing files to the new structure" saves a judgment call.

3. **File naming within the folder.** The prompt specifies the folder (`photos/dogs/{dog_id}/`) but not the file name. Should it be `{guid}.png`, `original_name.png`, or `primary.png` for the main photo? Stating "preserve original filename" or "use a UUID" removes ambiguity.

4. **"Both front and backend"** — the Blazor pages have no client-side file size/type enforcement. If adding `accept="image/png,image/jpeg"` and a `max` attribute to `<input type="file">` is desired on the HTML form, say so explicitly. The current change only enforces constraints server-side.

## Suggested improved prompt

> Move all dog photo storage to `wwwroot/photos/dogs/{dog_id}/`. Gallery photos use a UUID filename; the primary photo (single-replacement) uses the filename `primary.{ext}`. Persist the full URL path (e.g. `/photos/dogs/42/abc123.jpg`) in the existing `DogPhoto.Url` column — no schema change needed. Enforce server-side: PNG and JPG extensions only (not `.jpeg` or `.webp`), max 2 MB per file. Leave existing photos in `wwwroot/dogs/` untouched (their DB URLs still work). Also add `accept="image/png,image/jpeg"` to the file inputs in the Blazor edit page. Update `features/feature-dogs.md` UC-D5 and UC-D6 to reflect the new rules.
