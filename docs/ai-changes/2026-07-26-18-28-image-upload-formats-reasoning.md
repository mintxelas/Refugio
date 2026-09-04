# Reasoning — Allow any image format for uploads, JPG-only for the logo

## Problem
The UI/backend restricted photo uploads to JPG/PNG, and the shelter logo to PNG.
Requirement: accept **any** image format everywhere **except** the logo, which must be
**exclusively JPG**.

## Investigation
Traced the upload paths:
- Frontend file inputs (`ClientApp/src`): only three — `DogEdit.tsx`, `ExpenseEdit.tsx`
  (both already `accept="image/*"`), and `Settings.tsx` logo (`accept="image/png"`).
- Backend gate is the real filter. Every photo endpoint used the same pair:
  `ext is not (".jpg" or ".png")` + `PhotoFiles.HasValidImageBytes(file, ext)` — the true
  cause of the JPG/PNG-only behaviour. Endpoints: `Dog` (3), `Adoption` (2), `Finance/expense` (2),
  `Volunteer` (1).
- Logo endpoint (`SettingsEndpoints`) hard-coded `.png` and ran `ImageResizer.SaveSquareContainPng`,
  which decodes **PNG only** (the managed resizer has no JPEG decoder).

## Decisions
1. **Broaden the shared helper, not each call site's logic.** Replaced `HasValidImageBytes(file, ext)`
   with format-agnostic `HasImageBytes(file)` (magic-byte sniff for JPEG/PNG/GIF/BMP/WEBP) and added
   `IsImageExtension(ext)` for a broadened extension whitelist. Every endpoint now calls the two helpers
   — one behavioural change, applied uniformly.
2. **Logo = JPG only.** Extension check `.jpg/.jpeg` + `HasJpegBytes` magic-byte check; saved as `logo.jpg`.
3. **Dropped the PNG resizer for the logo.** The managed `ImageResizer` cannot decode JPEG, so the JPG is
   stored as-is and sized by existing CSS (`w-24 h-24 object-cover`). Marked with a `ponytail:` comment
   naming the upgrade path (add server-side resize when a JPEG decoder exists).

## Alternatives rejected
- **Add a JPEG decoder/resizer for the logo.** Overkill: no BCL JPEG decode on the target (Linux), and the
  logo already renders at a fixed CSS size. Rejected per YAGNI.
- **Per-endpoint extension lists.** Would duplicate the format policy across 8 handlers. Centralised in
  `PhotoFiles` instead.
- **Allow SVG.** Excluded — SVG is an XSS vector when served inline; "any raster image format" was the intent.

## Verification
- `PhotoFilesTests` (new): proves the extension whitelist and magic-byte detection for JPEG/PNG/GIF/BMP/WEBP,
  rejects non-images, and that `HasJpegBytes` rejects PNG (logo path).
- `SettingsApiTests`: updated the valid-upload case to JPEG bytes; the invalid-extension and anon cases
  still pass. Full `SettingsApiTests` (7) + `PhotoFilesTests` (11 with ImageResizer) green.
