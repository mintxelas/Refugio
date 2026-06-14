# Prompt Feedback: ui-spanish-translation

## Original prompt
> Translate the whole application UI and enums to spanish. Remove all the localization code, the frontend will only work in spanish.

## What worked
- "whole application UI" — clear scope.
- "frontend will only work in spanish" — confirmed no fallback needed.
- "Remove all the localization code" — unambiguous.

## Improvements for future prompts

1. **Clarify what "enums" means**: The prompt says "translate enums" but C# enum members are permanent API/DB identifiers. A better phrasing: "Translate the display labels for enum values in the UI — the C# member names must not change."

2. **Specify locale for dates**: The prompt didn't mention date/time locale. Adding "Use `es-ES` locale for all date and time formatting" avoids ambiguity.

3. **Specify number/currency format**: The prompt didn't address currency symbol or number formatting. Clarifying "keep `$` currency symbol and use period as decimal separator" (or specify the target format) prevents guessing.

4. **List out-of-scope items**: Explicitly stating "Do not change C# strings, API contracts, or database values" avoids any risk of accidentally modifying the backend.

## Example improved prompt
> Translate all user-visible text in the React SPA (`ClientApp/src/`) to Spanish. Rules:
> - No i18n library — hardcode Spanish strings directly.
> - No English fallback.
> - Enum display labels must be Spanish, but C# enum member names (API values) must not change.
> - Use `es-ES` locale for all date and time formatting.
> - Do not modify any C# code, API contracts, or database values.
