# Summary — Any image format for uploads; JPG-only logo

## What changed
| File | Change |
|---|---|
| `src/Refugio.Web/Helpers/PhotoFiles.cs` | Replaced `HasValidImageBytes(file, ext)` with `HasImageBytes(file)` (JPEG/PNG/GIF/BMP/WEBP magic bytes); added `IsImageExtension(ext)` broadened whitelist and `HasJpegBytes(file)` for the logo |
| `Endpoints/DogEndpoints.cs`, `AdoptionEndpoints.cs`, `FinanceEndpoints.cs`, `VolunteerEndpoints.cs` | Swapped `ext is not (".jpg" or ".png")` → `!PhotoFiles.IsImageExtension(ext)`; `HasValidImageBytes(file, ext)` → `HasImageBytes(file)` |
| `tests/Refugio.Tests.Integration/PhotoFilesTests.cs` | New — covers extension whitelist + magic-byte detection |

The logo stays **PNG-only, unchanged** (the user corrected the initial JPG request). `SettingsEndpoints`,
`Settings.tsx`, and `SettingsApiTests` keep their original PNG gate + `ImageResizer` 100×100 resize.
Dog/Expense frontend inputs already used `accept="image/*"`; no change needed there.

## Why
Requirement: accept any image format for all photo uploads. The backend extension/magic-byte gate was the
actual restriction, so the fix is centralised in `PhotoFiles`.

## Side effects / follow-ups
- Uploaded dog/expense/adoption/volunteer photos may now be `.gif/.webp/.bmp`; served statically, render in
  `<img>` fine.
- Logo behaviour is untouched (PNG, 100×100 resize).

## Code-review checklist
- [ ] `HasImageBytes` opens a fresh stream (position 0) — magic-byte read is reliable.
- [ ] Every photo endpoint calls `IsImageExtension` **and** `HasImageBytes` (defence: extension + content).
- [ ] Logo path unchanged: `.png` extension + `ImageResizer`.
- [ ] SVG intentionally excluded from `IsImageExtension` (XSS).
