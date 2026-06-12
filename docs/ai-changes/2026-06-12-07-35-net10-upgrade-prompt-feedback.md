# net10.0 upgrade — prompt feedback

## Original prompt

> perform a solution-wide net10 upgrade

## What worked

- Clear scope: "solution-wide" = all projects. Unambiguous.
- Short and direct — no over-specification left room to handle the one
  ASP.NET Core 10 behavioral change without asking.

## What was ambiguous

1. **Package scope** — "net10 upgrade" could mean only the TFM, or also bumping
   Microsoft.* version constraints. Inferred: bump `9.*` → `10.*` on Microsoft packages
   (they track the runtime version). Third-party packages (Akka.Hosting) were left at their
   current version since they're not TFM-coupled.
2. **Test failures** — one integration test asserted a redirect that ASP.NET Core 10 removed
   for API requests. No instruction on how to handle behavioral changes; fixed the test to
   match the correct new behavior.
3. **SDK pin** — no mention of creating or updating `global.json`. None exists in this repo;
   left as-is. If a pin is desired, say so explicitly.

## Suggested improved prompt

> Upgrade the entire solution from net9.0 to net10.0: update all TargetFramework elements and
> bump all Microsoft.* NuGet version constraints from 9.* to 10.*. Leave third-party packages
> unchanged. Fix any test failures caused by ASP.NET Core 10 behavioral changes. Run
> dotnet test and confirm 0 failures. Do not add a global.json pin.
