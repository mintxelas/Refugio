---
name: ef-migration
description: Create and apply an EF Core migration for a Refugio schema change. Use whenever a domain entity, property, enum-stored-as-string, or DbContext mapping changes and the database schema must follow. Covers the add/apply commands, the auto-migrate-on-startup behavior, and the enum-string no-DDL case.
---

# EF Core migration

Schema changes use proper EF Core migrations in `src/Refugio.Infrastructure/Migrations/`.
Startup runs `db.Database.Migrate()` automatically in `Program.cs`
(or `EnsureCreated()` when the provider is EF in-memory, detected via `db.Database.ProviderName`).

## Add a migration

```bash
dotnet ef migrations add <Name> --project src/Refugio.Infrastructure --startup-project src/Refugio.Web
```

Apply manually (usually unnecessary — startup migrates):
```bash
dotnet ef database update --project src/Refugio.Infrastructure --startup-project src/Refugio.Web
```

The SQLite DB (`shelter.db`) lives in `src/Refugio.Web/` and is auto-created on first run.

## Steps

1. Change the entity in `Refugio.Domain` and/or the mapping in `ShelterDbContext.OnModelCreating`.
2. Run `migrations add <DescriptiveName>`.
3. **Inspect the generated migration** — confirm it does what you intend (esp. renames; EF may emit
   drop+add instead of rename — fix by hand if so to avoid data loss).
4. Commit the migration file **and** the updated `ShelterDbContextModelSnapshot.cs`.
5. `dotnet build` — startup will apply it.

## Enum stored as string = no DDL needed

Enum properties use `HasConversion<string>()` (stored as TEXT). Adding a new enum **member** generates
an **empty** migration (snapshot updates, no `ALTER TABLE`). Still run `migrations add` to keep the
snapshot in sync.

⚠️ **Renaming an enum member is a breaking change** — existing DB string values won't match. Treat enum
member names as permanent identifiers.

## Notes
- Every schema change needs a committed migration — no automatic schema inference.
- New entity must also get `ISoftDeletable` + a global query filter (see add-soft-delete-entity skill).
