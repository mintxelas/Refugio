# Reasoning — Adoption photo gallery

## Goal

Implement a photo gallery for the `Adoption` aggregate following the same pattern as `ExpensePhoto` (no default photo, pure gallery). Storage: `wwwroot/photos/adoption/{adoptionId}/{guid}.{ext}`. Constraints: `.jpg/.png`, 2 MB max.

## Step-by-step logic

1. **Choose the right template.** Adoption photos have no "default" concept (unlike `DogPhoto`), so the `ExpensePhoto` pattern is the right template: simple child entity with `AdoptionId`, `Url`, `UploadedAt`.

2. **Domain layer first.** Create `AdoptionPhoto` entity. Add `ICollection<AdoptionPhoto> Photos { get; } = []` to `Adoption`. Add photo methods to `IAdoptionRepository`.

3. **Application contracts.** Add `AdoptionPhotoDto`. Add `List<AdoptionPhotoDto>? Photos = null` to `AdoptionDto` (optional, appended at end to preserve positional compatibility). Add photo interface methods to `IAdoptionService`.

4. **DtoMapping.** Add `AdoptionPhoto.ToDto()`. Update `Adoption.ToDto()` to pass `adoption.Photos.Select(p => p.ToDto()).ToList()` — works for both list queries (empty collection) and detail queries (photos loaded via Include).

5. **Infrastructure.** `ShelterDbContext`: add `DbSet<AdoptionPhoto>`, configure FK relationship with cascade delete, add soft-delete query filter. `AdoptionRepository`: implement photo methods; update `GetWithDogAsync` to `.Include(a => a.Photos)` so single-adoption detail returns photos.

6. **List queries don't include photos.** `GetAllAsync` and `GetPagedAsync` omit the Include — kanban board has many adoptions and doesn't need photo lists. `adoption.Photos` is initialized to `[]` so `ToDto()` maps to an empty list instead of null.

7. **Actors.** Add `GetAdoptionPhotos`, `AddAdoptionPhoto`, `RemoveAdoptionPhoto` messages. Register `Query<>` for get (concurrent), `Command<>` for add/remove (serialized per mailbox).

8. **Endpoints.** Three upload patterns matching existing dog/expense endpoints: SSR form upload, JSON upload, delete. `GET /api/adoptions/{id}/photos` for the list. `POST /api/adoptions/photos/{photoId}/delete` hard-deletes the row and the file on disk via `PhotoFiles.DeleteByUrl`.

9. **ShelterApiClient.** Add `GetAdoptionPhotos(int adoptionId)` for Blazor pages.

10. **Migration.** `dotnet ef migrations add AddAdoptionPhotos` — creates `AdoptionPhotos` table with FK → `Adoptions`, cascade delete, index on `AdoptionId`, `DeletedAt` nullable (soft-delete).

## Key decisions

- **No default photo.** Adoption gallery is a document gallery (photos of applicant, home inspection, etc.), not a profile display. `IsDefault` would add complexity with no benefit.
- **Photos included in single adoption fetch, not in list.** Kanban board performance matters; loading photos for every kanban card would be wasteful. `GetWithDogAsync` (used by `GET /api/adoptions/{id}`) includes them.
- **Hard delete on photo removal.** Same as `ExpensePhoto`: photos are files, not domain entities with history — hard delete + file deletion is correct.
