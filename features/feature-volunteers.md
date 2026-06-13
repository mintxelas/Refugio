# Feature: Volunteers

Registry of shelter volunteers, including optional login credentials, application role, profile photo, and preferred UI language. Credential rules live on the aggregate itself.

## Entity

### Volunteer (aggregate root)

| Property | Type | Notes |
|---|---|---|
| `Name` | string | required |
| `Email` | string | required; login identifier |
| `Phone` | string? | |
| `Role` | string | `Roles.Manager` or `Roles.Volunteer` (constants; normalized at startup) |
| `Status` | `VolunteerStatus` | default `Active` |
| `JoinDate` | DateTime | defaults to UTC now |
| `Notes` | string? | |
| `CanLogin` | bool | gates credentials |
| `PasswordHash` | string? | PBKDF2-SHA256 via `PasswordHelper`; **never serialized** (`VolunteerDto` has no PasswordHash) |
| `PreferredLanguage` | string? | culture name; only meaningful with login |
| `PhotoUrl` | string? | profile photo |

**Enum `VolunteerStatus`:** `Active`, `Inactive`, `Pending`.

**Behaviors / credential invariants:**
- `Volunteer.Register(...)` — static factory; applies credential rules.
- `Update(...)` — full edit; when `canLogin = false`, clears `PasswordHash` **and** `PreferredLanguage`; sets a new hash only when a non-blank password is supplied.
- `ChangeStatus(status)`, `ChangeRole(role)`, `SetPhotoUrl(url)`.
- `EnableLogin(password)` — turns login on and hashes.
- `VerifyPassword(password)` — false when no hash.
- `ChangePassword(current, new)` — succeeds only when the current password verifies.
- **Invariant:** no login → no password hash and no preferred language.

## Use cases

### UC-V1: Browse volunteers
- **UI:** `/volunteers` — status filter, pagination, counts cards.
- **API:** `GET /api/volunteers?status=`, `GET /api/volunteers/paged?status=&page=&pageSize=` (default 25), `GET /api/volunteers/counts` (`VolunteerCounts` read model).

### UC-V2: Register a volunteer
- **API:** `POST /api/volunteers` → 201. Optional credentials (`CanLogin`, password, preferred language).

### UC-V3: Edit a volunteer
- **UI:** `/volunteers/{id}` — credentials, role, photo, preferred language.
- **API:** `PUT /api/volunteers/{id}` → 200/404.

### UC-V4: Activate / deactivate
- **API:** `PUT /api/volunteers/{id}/status` (REST), or Manager-only UI forms `POST /api/volunteers/{id}/activate` / `POST /api/volunteers/{id}/deactivate` (redirect `/volunteers`).

### UC-V5: Profile photo
- **API:** `POST /api/volunteers/{id}/photo` (multipart `Photo`; `.jpg/.png` only, max 2 MB; stored as `wwwroot/photos/volunteer/{id}/primary.{ext}`, previous primary deleted); `POST /api/volunteers/{id}/photo/delete` clears URL and deletes `primary.*` from the volunteer's folder. Both auth-required.

### UC-V6: Delete / restore / purge (Manager)
- **API:** `DELETE /api/volunteers/{id}` (204/404), `POST /api/volunteers/{id}/delete` (redirect `/volunteers`), `/restore`, `/purge` (redirect `/admin/deleted?tab=volunteers`). Deleted listings: `GET /api/volunteers/deleted[/{id}]`.

### UC-V7: Login (see [Auth](feature-auth.md))
- `IVolunteerService.LoginAsync(email, password)` backs `POST /auth/login`; only `CanLogin` volunteers with a verifying password succeed.
