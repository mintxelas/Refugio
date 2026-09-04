# Site backup ZIP - prompt feedback

## Original prompt
> Make a plan of how to implement a backup of the site and download it as a zip file that we can launch from the configuration area of the site. It must include the database and all the uploaded pictures.
> (follow-up) yes, take it from shelterdbcontext. build it.

## What was clear
- Deliverable (a downloadable ZIP), scope (database + uploaded pictures), and entry point (configuration/Settings area) were all explicit. Good.

## What needed a judgment call
1. **Backup vs restore.** The prompt says "backup" only. Assumed no restore/upload flow. State it if restore is wanted.
2. **Consistency requirement.** Not specified. Chose the SQLite online-backup API over `File.Copy` to avoid a torn snapshot under concurrent writes. Worth stating if a plain file copy is acceptable.
3. **Who may download.** Not specified. Gated to `Manager` (a backup includes password hashes and all data). Confirm if Volunteers should also have it.
4. **Connection-string source.** Resolved mid-conversation ("take it from shelterdbcontext"). Naming the source up front avoids the hardcoded-path-vs-config round trip.

## How to make future prompts sharper
- Name the permission level ("Managers only").
- Say whether restore is in scope now or later.
- Note any size/consistency constraints ("DB may be written during backup", "photos can be several GB").
- If a specific config source or path is required, state it in the first message.

Suggested one-liner:
> Add a Manager-only "Download backup" button in Settings that streams a ZIP of the live database (consistent snapshot) plus all uploaded pictures. Backup only, no restore.
