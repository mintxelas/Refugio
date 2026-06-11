# Feature: Settings & Branding

Single-row aggregate holding the shelter's branding: name, phrase, and logo. Cached in-process (`SettingsCacheService`) and invalidated on every update.

## Entity

### ShelterSettings (aggregate root, single row)

| Property | Type | Notes |
|---|---|---|
| `Name` | string | default `"Haven Sanctuary"` |
| `Phrase` | string? | tagline, default `"City Main Branch"` |
| `LogoUrl` | string? | `/branding/logo.png?v={ticks}` |

**Behaviors:** `ShelterSettings.CreateDefault(...)`, `Update(name, phrase)`, `SetLogo(logoUrl)`.

## Use cases

### UC-S1: View settings
- **UI:** `/settings` (page visible to all authenticated users; edits Manager-only).
- **API:** `GET /api/settings` (auth) → `SettingsDto`.

### UC-S2: Update name / phrase (Manager)
- **API:** `PUT /api/settings` → 200; invalidates `SettingsCacheService` so the layout header refreshes.

### UC-S3: Upload logo (Manager)
- **API:** `POST /api/settings/logo` (multipart `Logo`, antiforgery disabled).
- **Rules:**
  - **PNG only** — the resize pipeline is fully managed PNG (no GDI+, Linux-compatible) and PNG carries the transparency the padded output needs.
  - Max 5 MB; violations redirect `/settings?logoError=1`.
  - Image is normalized to a **100×100 PNG**: scaled to longest side, centered, transparent padding (`ImageResizer.SaveSquareContainPng`).
  - Prior logo files (`logo.*` in `wwwroot/branding`) are deleted first; unreadable image content also redirects with `logoError=1`.
  - Stored URL gets a `?v={ticks}` cache-bust suffix because the filename is stable.
  - Cache invalidated after save.
