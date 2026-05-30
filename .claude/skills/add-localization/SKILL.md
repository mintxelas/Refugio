---
name: add-localization
description: Add or change a localized UI string or enum display key in the Refugio app. Use whenever adding visible text to a .razor page, a validation message, or a new enum member that renders in the UI. Enforces editing all four RESX culture files and the EnumType_MemberName convention. Trigger when you'd otherwise hardcode UI text.
---

# Add a localization string

**Never hardcode UI text in Razor.** Every visible string goes through `@L["Key"]`.

Four cultures — add the key to **all four** RESX files (`src/Refugio.Web/Resources/`):
`SharedResources.resx` (en-US, default), `SharedResources.es-ES.resx`, `SharedResources.pt-BR.resx`,
`SharedResources.ca-ES.resx`.

## Steps

1. Pick a key following the `Section_KeyName` convention (e.g. `Validation_NameRequired`, `Dogs_Title`).
   Avoid collisions — there are 345+ keys.
2. Add `<data name="Key"><value>…</value></data>` to **all four** RESX files with the translated text.
3. Use it:
   - Markup: `@L["Key"]`
   - C#: `L["Key"].Value`
   - Parameterized: `string.Format(L["Key"].Value, arg)`

`L` is injected globally (`_Imports.razor`: `@inject IStringLocalizer<SharedResources> L`).
Marker class: `src/Refugio.Web/SharedResources.cs`.

## Enum display keys — convention `EnumType_MemberName`

Storage/POST value stays the C# member name (English); only the **display** is localized.

| Enum | Key pattern |
|---|---|
| `DogStatus` | `DogStatus_{value}` (via `DogHelpers.DogStatusDisplay(s, L)`) |
| `AdoptionStatus` | `Adoptions_{value}` |
| `AdoptionType` | `Adoptions_Adoption` / `Adoptions_Foster` |
| `VolunteerStatus` | `VolunteerStatus_{value}` |
| `DonationCategory` | `DonationCategory_{value}` |
| `ExpenseCategory` | `ExpenseCategory_{value}` |

Adding a new enum **member** → add its `EnumType_Member` key to all four RESX **before** using it in UI
(missing key silently falls back to the literal key string — visible but not a crash).

`<select>` `value` attributes = enum member name; only the label is localized:
```razor
<option value="@cat">@L[$"ExpenseCategory_{cat}"]</option>
```

## Adding a culture
Add it to `supportedCultures` in `Program.cs` — `/set-language` and the `MainLayout.razor` switcher
honor any culture in that list automatically. Create the matching `SharedResources.<culture>.resx`.

## Verify
- Key exists in all four RESX files.
- No raw literal text or `.ToString()` of an enum in the Razor.
