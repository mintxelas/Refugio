# Feature: Localization

Fully localized UI across four cultures, with a language switcher and a per-volunteer preferred language applied at login.

## Model

- **Supported cultures:** `en-US` (default), `es-ES`, `pt-BR`, `ca-ES`.
- **Persistence:** culture cookie via `CookieRequestCultureProvider` (1-year expiry).
- **Resources:** single shared resource — `src/Refugio.Web/Resources/SharedResources*.resx` (4 files), marker class `SharedResources.cs`, `IStringLocalizer<SharedResources> L` injected globally in `_Imports.razor`.
- **Usage:** `@L["Key"]` in markup, `L["Key"].Value` in C#, `string.Format(L["Key"].Value, arg)` for parameterized strings.
- **Enum display:** key convention `EnumType_MemberName` (e.g. `L[$"DonationCategory_{d.Category}"]`); `DogStatus` goes through `DogHelpers.DogStatusDisplay`. `<select>` option **values** stay C# member names (the wire/parse format) — only labels are localized.
- **Rule:** no hardcoded UI text anywhere; every visible string, validation message, and enum display value exists in all four RESX files.

## Use cases

### UC-L1: Switch language manually
- **UI:** language switcher in the layout.
- **API:** `GET /set-language?culture=&returnUrl=` — only honors cultures in the supported list; sets the culture cookie and redirects back (defaults to `/`).

### UC-L2: Preferred language at login
- When a volunteer with a supported `PreferredLanguage` logs in, the culture cookie is set automatically (see [Auth](feature-auth.md) UC-AU1).

### UC-L3: Localized validation and enums
- Server-side validation messages come from RESX; enum labels render via the `EnumType_MemberName` convention while form posts round-trip the C# member name.
