---
name: blazor-ssr-form-page
description: Build or edit a Blazor SSR page with a form, server-side validation, and POST handling in the Refugio app. Use when adding/editing a .razor page that takes user input — create/edit forms, delete buttons, filters, tabs, pagination. Covers the pure-static-SSR constraints (no @onclick/@bind), FormReader parsing, Validator helper, and POST-then-redirect.
---

# Blazor SSR form page

**Pure static SSR — no interactive render mode.** `@onclick`, `@bind`, and all interactive Razor
directives are **silently ignored**. Every user action is a full HTTP round-trip.

## Form skeleton

```razor
<form method="post" @formname="unique-name">
    <AntiforgeryToken />
    <input name="Name" value="@_name" />
    <button type="submit">Save</button>
</form>
```

## Read POST in `OnInitializedAsync` — NEVER `[SupplyParameterFromForm]`

`[SupplyParameterFromForm]` silently drops fields that can't bind (empty string → `int`). Use `FormReader`:

```csharp
var ctx = HttpContextAccessor.HttpContext;
if (ctx?.Request.Method == "POST")
{
    var form = await ctx.Request.ReadFormAsync();
    var name = FormReader.GetString(form, "Name");
    var qty  = FormReader.GetInt(form, "Count");
    var cat  = FormReader.GetEnum(form, "Category", ExpenseCategory.Other);
    // validate, then call actor
}
```

`FormReader` (`Helpers/FormReader.cs`) typed helpers: `GetString`, `GetStringOrNull`, `GetInt`,
`GetNullableInt`, `GetDecimal`, `GetDateTime`, `GetBool`, `GetEnum<T>`. Globally imported.

## Server-side validation (HTML `required` is bypassed by raw HTTP)

Accumulate errors, display above form, bail before calling actor. Use the shared `Validator` helper
(`Helpers/Validator.cs`): `RequireNotEmpty`, `RequirePositive`, `RequireNonNegative`,
`RequireValidEmail`, `RequireDate`, `RequireAfter`.

```csharp
_errors.Clear();
if (string.IsNullOrWhiteSpace(name)) _errors.Add(L["Validation_NameRequired"].Value);
if (amount <= 0) _errors.Add(L["Validation_AmountPositive"].Value);
if (_errors.Count > 0) return;   // keep sticky field values for re-render
```
Add any new validation message keys to **all** RESX files (see add-localization skill).

## After a successful POST — redirect to prevent re-submit

```csharp
Nav.NavigateTo("/dogs/" + id);   // 302, stops refresh re-submitting
```

## Delete buttons = POST form + antiforgery (NOT a GET link)

```razor
<form method="post" action="/api/dogs/@item.Id/delete">
    <AntiforgeryToken />
    <button type="submit" onclick="return confirm('Delete?')">Delete</button>
</form>
```
Endpoints: REST uses real `DELETE`; Blazor uses parallel `POST /api/{entity}/{id}/delete`.
All action endpoints `.RequireAuthorization()`; destructive ones `.RequireAuthorization("Manager")`.
Hide/disable destructive UI for non-managers via `<AuthorizeView Roles="Manager">`.

## Enum `<select>`

```razor
@foreach (var cat in Enum.GetValues<ExpenseCategory>())
{
    <option value="@cat">@L[$"ExpenseCategory_{cat}"]</option>
}
```
`value` = C# member name (so `FormReader.GetEnum` parses it). Label = localized. Never render raw `.ToString()`.

## Other SSR patterns
- **Query params / filters / tabs / pagination:** `[SupplyParameterFromQuery]` + `<a href="?param=x">` links.
- **Multiple forms on one page:** distinguish via `form["_handler"].ToString()` (injected by `@formname`).
- **Collapsible:** `<details>/<summary>`. **JS confirm:** `onclick="return confirm(...)"`. No other JS.
- **Clickable rows:** absolute `<a class="absolute inset-0">` in a `relative` container; content `pointer-events-none`; action buttons `relative z-10`.

## Never
- Hardcode UI text — use `@L["Key"]` (see add-localization).
- Use arbitrary hex — use the named Material-3 tokens from `App.razor`.
