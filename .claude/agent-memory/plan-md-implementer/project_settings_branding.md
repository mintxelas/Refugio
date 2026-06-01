---
name: project-settings-branding
description: Notes from implementing the shelter branding / settings feature (PLAN.md in features/shelter-branding/)
metadata:
  type: project
---

Implemented in conversation on 2026-06-01.

**Feature:** Manager-only /settings page for shelter branding (logo, name, tagline). ShelterSettings entity, SettingsActor, ISettingsMessage routing, SettingsEndpoints, Settings.razor, MainLayout dynamic branding, App.razor title changed to "Refugio".

**Why:** Part of shelter-branding feature plan. Allows managers to customize the sidebar header branding without code changes.

**Key implementation decisions:**

1. SettingsActor sender-capture pattern: `var sender = Sender;` must be captured BEFORE entering `WithDb(async db => ...)` because `Sender` is only valid synchronously (before any await). This is the same pattern as `ShelterActorBase.SoftDelete<T>`, but the plan's code samples showed direct `Sender.Tell()` inside the lambda which only works in FinanceActor because those handlers also capture or access `Sender` in the right scope.

2. `using Akka.Actor;` import required in actor files that call `sender.Tell(...)` — without it, the `IActorRef.Tell(object)` extension method is not found, giving CS7036.

3. **SettingsCacheService** (singleton + IMemoryCache) was added as an improvement over the plan's "per-render actor Ask" approach. The plan explicitly calls this out as the documented future optimization. Without caching, each page render fired a new actor Ask, which caused PaginationTests to fail with actor timeout (10s) due to mailbox pressure in the shared integration test factory. With a 60-second TTL cache, all 145 integration tests pass.

4. Cache invalidation: SettingsCacheService.Invalidate() called in: Settings.razor after UpdateSettings, SettingsEndpoints after logo upload, SettingsEndpoints after name/phrase update via the form endpoint.

5. `StringValues == "1"` comparison in Razor is ambiguous — use `.ToString() == "1"` instead.

**How to apply:** When adding a new actor call to a layout or shared component that renders on every page, always use a cache layer to avoid per-render actor contention in integration tests.
