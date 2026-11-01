# SitePack persistence and state

`Program.cs` registers storage, state, JSON backup, project, material, and transaction
services as scoped services. Blazor WebAssembly shares these instances within one tab.
Storage uses built-in localStorage interop without a backend or additional packages.
`wwwroot/js/sitepack.js` supplies only browser-local dates and JSON file downloads.

## Component integration

Inject `AppState`, subscribe to `Changed`, and call `InitializeAsync` from the
component lifecycle when JS interop is available. In the change handler, use
`InvokeAsync(StateHasChanged)`; unsubscribe when the component is disposed.
Catch `SitePackDataException` and display its Persian `Message`. `LastError` also
exposes the latest state operation error. Never turn a load failure into an empty save.

`Snapshot`, `ActiveMaterials`, and `ActiveTransactions` are detached copies.
Use `UpdateAsync(candidate => { ... })` to edit a candidate document. The state
serializes operations, validates the candidate, awaits persistence, and then
publishes the new state. A failed write retains the previous committed state.
Callbacks must only edit their candidate and must not call state operations.
`Changed` handlers should schedule rendering and should not throw exceptions.

`SelectProjectAsync(projectId)` validates and persists selection. A null selection
returns empty active-project query results. `MaterialService` calculates stock
from transactions and never persists a separate stock count. Business services
validate ownership inside the state update and preserve creation timestamps on edits.
Material units cannot change after transactions exist.

## JSON backup and restore

`IJsonBackupService.ExportJsonAsync()` returns the complete versioned JSON string;
the settings page downloads it as a UTF-8 JSON file. `RestoreJsonAsync(json)`
validates it first, asks for confirmation with the browser's Persian `confirm`
dialog, and replaces storage/state only after confirmation and a successful write.
It returns false on cancellation and throws a Persian exception for invalid data
or failed browser operations. Restore can recover from corrupt existing JSON.

All data uses `sitepack:data:v1`. Missing storage initializes a valid empty document;
invalid existing storage is preserved. Required fields, duplicate keys/IDs/names,
schema, references, units, quantities, UTC timestamps, calendar dates, phone text,
and usage work areas are checked before saving. JSON keeps Gregorian dates;
UI formatting uses `PersianDate` and `PersianText` for Jalali dates and Persian digits.

Generic state updates block deletion of materials that already have transactions,
their transfer between projects, and deletion of active or populated projects.
Confirmed backup restore is a deliberate full replacement. UI forms ask for
confirmation before transaction edits/deletions and project/material deletions.

LocalStorage is browser-local and quota-limited. Closing/reopening retains saved
data, but clearing browser storage removes it. Multiple tabs do not synchronize
in-memory state; concurrent tab writes can overwrite each other. This service
does not add PWA asset caching or guarantee offline application startup.

## Verification

Run from the project root:

```powershell
dotnet build
dotnet run --project tests/SitePack.StorageChecks/SitePack.StorageChecks.csproj
```

The console checks link the production sources and simulate JS interop without
third-party test packages. They cover reloads, project isolation, concurrent
updates, snapshot isolation, storage failures, malformed backups, and restore
confirmation. There are now 50 console checks, including calendar and business-service
coverage. `tests/browser-checks.cjs` additionally exercises the UI in headless Edge;
see `UI_REVIEW.md` for setup and the verified browser workflows.
