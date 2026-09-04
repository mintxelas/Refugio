# Prompt feedback

## Original prompt
> La UI de la aplicación sólo permite subir archivos jpg. Cambiala para que permita subir
> cualquier formato de archivo de imagen como imagen en todas partes EXCEPTO en el logotipo
> de la página. El logotipo debe ser exclusivamente un jpg.

## What worked
- Clear intent: broaden everywhere, restrict the logo.
- The explicit exception (logo) prevented an over-broad change.

## Where it could be sharper
1. **Premise was slightly off.** The photo inputs already accepted `image/*` on the frontend and
   JPG/PNG on the backend; the logo was PNG (not JPG-restricted UI-wide as stated). Naming the actual
   symptom ("webp/gif uploads are rejected", "logo only accepts PNG") points straight at the gate.
2. **"Any image format" is ambiguous.** It excluded SVG here (XSS risk) and covers raster formats
   (JPEG/PNG/GIF/BMP/WEBP). State whether vector/SVG and animated formats are wanted.
3. **Hidden constraint not flagged.** The logo pipeline used a PNG-only resizer, so "logo must be JPG"
   forced dropping server-side resize. Calling out "keep the 100×100 resize" or "raw is fine" would have
   removed a judgement call.
4. **Scope of "en todas partes".** Only Dog/Expense have UI inputs; Adoption/Volunteer/Finance endpoints
   exist without SPA inputs. Say whether backend-only endpoints are in scope (I included them).

## Suggested rewrite
> Photo uploads (dogs, expenses, adoptions, volunteers) currently reject anything but JPG/PNG at the
> backend. Accept any **raster** image (JPEG/PNG/GIF/BMP/WEBP; no SVG). The shelter logo must accept
> **only JPG** — dropping the current PNG 100×100 resize is fine; store the JPG as-is. Update frontend
> `accept` attributes and validation messages to match.
