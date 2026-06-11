# Feature: Dogs

Catalog and lifecycle of the shelter's dogs: check-in, profile management, status tracking, and a photo gallery. `Dog` is the aggregate root; `MedicalRecord`, `Medication` (see [Medical](feature-medical.md)) and `DogPhoto` are its children, reachable only through `IDogRepository`.

## Entities

### Dog (aggregate root)

| Property | Type | Notes |
|---|---|---|
| `Name` | string | required |
| `Breed` | string | required |
| `AgeMonths` | int | age expressed in months |
| `Gender` | string | free text ("Male"/"Female") |
| `Status` | `DogStatus` | default `Available` |
| `PhotoUrl` | string? | primary photo (mirror of the default gallery photo) |
| `Traits` | string? | personality traits |
| `Notes` | string? | |
| `ArrivalDate` | DateTime | defaults to UTC now at check-in |
| `WeightKg` | decimal | |
| `MedicalRecords`, `Medications`, `Adoptions`, `Photos` | collections | child navigation |

**Behaviors:** `Dog.CheckIn(...)` (static factory), `UpdateDetails(...)` (full edit incl. status), `SetPhotoUrl(...)`.

**Enum `DogStatus`:** `Available`, `Adopted`, `Foster`, `Medical`, `Quarantine`.

### DogPhoto (child entity)

| Property | Type | Notes |
|---|---|---|
| `DogId` | int | parent |
| `Url` | string | `/dogs/{file}` under wwwroot |
| `IsDefault` | bool | at most one default per dog |
| `UploadedAt` | DateTime | UTC now |

**Behaviors:** `DogPhoto.Create(dogId, url, isDefault)`, `SetDefault(bool)`.

## Use cases

### UC-D1: Browse dog catalog
- **Actor:** any authenticated user.
- **UI:** `/dogs` — search box, status filter, pagination (query params).
- **API:** `GET /api/dogs?search=&status=`, `GET /api/dogs/paged?search=&status=&page=&pageSize=` (default 20/page).

### UC-D2: View dog detail
- **UI:** `/dogs/{id}` — profile, medical history, medications, photo gallery.
- **API:** `GET /api/dogs/{id}` (404 when missing or soft-deleted).

### UC-D3: Check in a new dog
- **UI:** `/dogs/new` (`DogCheckin.razor`) — POST form, server-side validation, sticky values on error.
- **API:** `POST /api/dogs` → 201 Created with `DogDto`.
- **Domain:** `Dog.CheckIn(...)`; arrival date defaults to now.

### UC-D4: Edit dog
- **UI:** `/dogs/{id}/edit` — all fields including status; photo gallery management.
- **API:** `PUT /api/dogs/{id}` → 200 or 404.
- **Domain:** `Dog.UpdateDetails(...)`.

### UC-D5: Upload / replace primary photo
- **API:** `POST /api/dogs/{id}/photo` (multipart field `Photo`, antiforgery disabled, auth required).
- **Rules:** extensions `.jpg/.jpeg/.png/.webp` only; max 5 MB; replaces any existing `{id}.*` file in `wwwroot/dogs`; redirects back to the edit page (silently on violation).

### UC-D6: Manage photo gallery
- **API:**
  - `GET /api/dogs/{id}/photos` — list gallery.
  - `POST /api/dogs/{id}/photos` — multi-file upload (field `Photos`), same extension/size rules per file, names `{dogId}_{guid}.{ext}`.
  - `POST /api/dogs/photos/{photoId}/default?dogId=` — mark default (auth).
  - `POST /api/dogs/photos/{photoId}/delete?dogId=` — remove photo and delete the file from disk (auth).
- **Rule:** at most one default photo per dog.

### UC-D7: Delete / restore / purge dog (Manager)
- **API:** `DELETE /api/dogs/{id}` (REST, 204/404), `POST /api/dogs/{id}/delete` (UI form → redirect `/dogs`), `POST /api/dogs/{id}/restore` and `POST /api/dogs/{id}/purge` (redirect `/admin/deleted?tab=dogs`).
- **Rules:** delete is a soft delete (interceptor); purge removes permanently. Children of a deleted dog cannot be restored while the parent is deleted (see [Admin](feature-admin-deleted.md)).
- **Deleted listings:** `GET /api/dogs/deleted`, `GET /api/dogs/deleted/{id}` (Manager).
