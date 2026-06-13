# Prompt Feedback: Dog Date of Entry

## Original prompt

> Ensure the dog entity has a "Date of Entry" field (mandatory) and that it is available in all related forms and views.

## What worked

Clear goal, clear scope ("forms and views"), clear constraint ("mandatory").

## What would improve it

1. **Map the field name**: "Date of Entry" maps to `ArrivalDate` in the codebase — this required investigation. Say "expose the existing `ArrivalDate` field" or "add a Date of Entry field (store as `ArrivalDate`)" to remove ambiguity.

2. **List the specific forms and views**: The prompt says "all related forms and views." Enumerating them removes ambiguity:
   - Check-in form (`DogCheckin.tsx`)
   - Edit form (`DogEdit.tsx`)
   - Detail view (`DogDetail.tsx`)

3. **State if API contract matters**: The change required updating `CreateDogRequest` and `UpdateDogRequest`. A note like "the field must be accepted by the API and not default server-side" makes the mandatory requirement precise at every layer.

4. **Specify date-only vs datetime**: Should the UI show a date picker (no time) or a datetime picker? "Date of Entry" implies date-only — saying so explicitly avoids design guessing.

## Improved prompt example

> The `Dog` entity already has an `ArrivalDate` field but it is not exposed to callers. Make it mandatory and editable end-to-end:
> - Add `ArrivalDate` (DateTime, mandatory, no server-side default) to `CreateDogRequest` and `UpdateDogRequest`
> - Show a date-only input (pre-filled with today on create, loaded from the DTO on edit) in `DogCheckin.tsx` and `DogEdit.tsx`
> - Display the date in `DogDetail.tsx` (already rendered — verify)
> - Update all unit and integration tests that construct dog requests
