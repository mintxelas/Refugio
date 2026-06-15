# Reasoning: OpenAPI/Swagger + Scalar UI

## Steps

1. Checked `Program.cs` and `.csproj` — no existing OpenAPI setup.
2. .NET 10 ships `Microsoft.AspNetCore.OpenApi` as a first-party package (introduced .NET 9). Chose it over Swashbuckle because it is the Microsoft-maintained path and has no XML doc dependency.
3. Added `Scalar.AspNetCore` for the browser UI — modern, actively maintained, integrates with `.MapOpenApi()` in one call.
4. Registered `AddOpenApi()` with a document transformer to set title/version.
5. Mapped `/openapi/v1.json` (via `MapOpenApi()`) and `/scalar/v1` (via `MapScalarApiReference()`) inside `IsDevelopment` guard — no production exposure.

## Alternatives rejected

- **Swashbuckle** — unmaintained for .NET 9+; Microsoft stopped recommending it.
- **NSwag** — heavier, generates client code; overkill for a browse-only UI.
- **Exposing in production** — shelter API is not a public API; dev-only is correct.
