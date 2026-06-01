---
name: pattern-file-upload
description: File upload precedent in Refugio — store file on disk under wwwroot, persist only the URL string in the DB
metadata:
  type: project
---

Canonical file-upload precedent: dog photo. Form in DogEdit.razor is `<form method="post" action="/api/dogs/{id}/photo" enctype="multipart/form-data">` with `<input type="file" name="Photo" accept=".jpg,.jpeg,.png,.webp">`.

Endpoint (DogEndpoints.cs, /api/dogs/{id}/photo): reads ctx.Request.Form.Files.GetFile("Photo"); validates extension is jpg/jpeg/png/webp and size <= 5MB; writes to Path.Combine(env.WebRootPath, "dogs") via IWebHostEnvironment; deletes old files matching {id}.*; saves as {id}{ext}; then actors.Ask(new UpdateDogPhoto(id, "/dogs/{file}")) to persist the URL STRING in the DB (Dog.PhotoUrl). Redirects back. Pipeline: `.RequireAuthorization().DisableAntiforgery()`.

Key convention: the IMAGE BYTES live on disk under wwwroot; only the relative URL string is stored in the entity. Actors never do file I/O — the endpoint does the disk write, the actor persists the path.

See [[pattern-new-actor-area]].
