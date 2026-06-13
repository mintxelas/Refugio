# Summary — Adoption photo gallery

## New files

| File | Purpose |
|---|---|
| `src/Refugio.Domain/Entities/AdoptionPhoto.cs` | Child entity: `AdoptionId`, `Url`, `UploadedAt` |
| `src/Refugio.Infrastructure/Migrations/20260613212749_AddAdoptionPhotos.cs` | Creates `AdoptionPhotos` table |

## Modified files

| File | Change |
|---|---|
| `src/Refugio.Domain/Entities/Adoption.cs` | Added `ICollection<AdoptionPhoto> Photos { get; } = []` |
| `src/Refugio.Domain/Repositories/IAdoptionRepository.cs` | Added `GetPhotosAsync`, `GetPhotoAsync`, `AddPhoto`, `RemovePhotoPermanently` |
| `src/Refugio.Application/Contracts/AdoptionContracts.cs` | Added `AdoptionPhotoDto`; added `List<AdoptionPhotoDto>? Photos` to `AdoptionDto` |
| `src/Refugio.Application/Services/AdoptionService.cs` | Added `GetAdoptionPhotosAsync`, `AddAdoptionPhotoAsync`, `RemoveAdoptionPhotoAsync` to interface and impl |
| `src/Refugio.Application/Mapping/DtoMapping.cs` | Added `AdoptionPhoto.ToDto()`; updated `Adoption.ToDto()` to include photos |
| `src/Refugio.Infrastructure/Data/ShelterDbContext.cs` | Added `DbSet<AdoptionPhoto>`, FK config, query filter |
| `src/Refugio.Infrastructure/Repositories/AdoptionRepository.cs` | Added photo methods; `GetWithDogAsync` now includes `Photos` |
| `src/Refugio.Actors/Messages/AdoptionMessages.cs` | Added `GetAdoptionPhotos`, `AddAdoptionPhoto`, `RemoveAdoptionPhoto` |
| `src/Refugio.Actors/AdoptionActor.cs` | Registered photo message handlers |
| `src/Refugio.Web/Endpoints/AdoptionEndpoints.cs` | Added `GET/POST /adoptions/{id}/photos`, `POST .../photos/upload`, `POST .../photos/{photoId}/delete` |
| `src/Refugio.Web/Services/ShelterApiClient.cs` | Added `GetAdoptionPhotos(int adoptionId)` |
| `features/feature-adoptions.md` | Added `AdoptionPhoto` entity, `Photos` on `Adoption`, UC-A6b |

## Behavior

- **Storage:** `wwwroot/photos/adoption/{adoptionId}/{guid}.{ext}`
- **Extensions:** `.jpg` and `.png` only
- **Size:** 2 MB max per file
- **Delete:** hard-deletes DB row + file on disk
- **List API:** `GET /api/adoptions/{id}/photos`
- **Upload (SSR):** `POST /api/adoptions/{id}/photos` (field `Photos`, redirects to `/adoptions/{id}`)
- **Upload (JSON):** `POST /api/adoptions/{id}/photos/upload` → `{ urls: [...] }`
- **Delete:** `POST /api/adoptions/photos/{photoId}/delete?adoptionId=`
- **`AdoptionDto.Photos`** is populated when fetching single adoption; empty list in kanban/list views

## Code review checklist

- [ ] `AdoptionPhoto` entity CLR namespace is `Refugio.Domain.Entities`
- [ ] Soft-delete query filter on `AdoptionPhoto` in `ShelterDbContext`
- [ ] FK cascade delete configured (orphan photos auto-deleted when adoption purged)
- [ ] `GetWithDogAsync` includes `.Include(a => a.Photos)`
- [ ] List queries (`GetAllAsync`, `GetPagedAsync`) do NOT include photos
- [ ] `RemovePhotoPermanently` sets `SkipSoftDeleteInterceptor = true`
- [ ] Migration table name is `AdoptionPhotos`, FK → `Adoptions`
- [ ] Photo endpoints: `.jpg/.png`, 2 MB, `RequireAuthorization()`
