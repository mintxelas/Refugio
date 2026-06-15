# Summary: OpenAPI/Swagger + Scalar UI

## What changed

- `Refugio.Web.csproj`: added `Microsoft.AspNetCore.OpenApi 10.0.9` and `Scalar.AspNetCore 2.16.3`.
- `Program.cs`: `AddOpenApi()` registration (title "Refugio Shelter API", version "v1"); `MapOpenApi()` + `MapScalarApiReference()` in dev-only block.

## URLs (dev only)

| Resource | URL |
|---|---|
| OpenAPI JSON | `http://localhost:5110/openapi/v1.json` |
| Scalar UI | `http://localhost:5110/scalar/v1` |

## Side effects / follow-up

- Adding `.WithName("OperationId")` and `.WithSummary("…")` to endpoint definitions will enrich the spec (optional, zero-friction to add later).
- The spec auto-discovers all minimal API endpoints and their parameter/body types from reflection — no XML doc files needed.

## Review checklist

- [ ] Scalar UI loads at `/scalar/v1` in dev.
- [ ] All endpoint groups appear in the spec.
- [ ] Production build does not expose the spec (guarded by `IsDevelopment`).
