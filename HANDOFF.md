# SitePack developer handoff

Updated: November 2, 2026 (Asia/Tehran).

## Start here

Read `AGENTS.md`, `PRD.md`, and `SPEC.md` in the project root before implementing
changes. `Services/README.md` explains the persistence APIs and component integration.

SitePack (سایت‌پَک) tracks construction materials across projects. The app is
client-side Blazor WebAssembly targeting `net10.0`, with namespace
`Material_Tracker`. Work is incremental: briefly explain each file's purpose
before creating or changing it, complete the authorized phase, and obtain the
user's approval before proceeding into another implementation phase.

The core models, persistence layer, and UI are implemented. The user also authorized
and received the PWA upgrade: manifest, icons, service-worker caching, and offline
startup verification. See `UI_REVIEW.md` for the UI file index and screenshots.
CSV export remains future work. Continue within the user's authorized task scope.

## Current handoff status

| Area | Status |
| --- | --- |
| Core models and localStorage persistence | Implemented and approved by the user. |
| Date utilities and business services | Implemented to support the UI; ownership, history, and unit protections covered by checks. |
| Persian RTL UI | Implemented and reported to the user; awaiting explicit review/approval. |
| Installable PWA and offline startup | Implemented; published offline startup verified at `/` and `/Material_Tracker/`. Native installation on physical devices remains untested. |
| Build | Last implementation build passed with zero warnings and zero errors. |
| Console checks | 50/50 passed, including the original 42 storage checks and eight added utility/service checks. |
| Browser checks | Earlier UI verification passed 11 workflow groups. New PWA checks passed at both base paths in headless Edge with no browser exceptions. |
| Review artifacts | Desktop/mobile dashboard and transaction-form screenshots exist locally. |
| Development server | Stopped after verification; start it again for review. |
| Current request | Update this handoff to record the completed PWA upgrade and verification. |

This handoff update changes documentation only. Build, publish, storage, and PWA
verification results are from the completed PWA upgrade on November 2, 2026;
the 11-group UI browser results are from the earlier UI implementation. Tests
were not rerun for this documentation update.

## Resume and run

From the repository root:

```powershell
dotnet build
dotnet run --project tests/SitePack.StorageChecks/SitePack.StorageChecks.csproj
dotnet run --no-build --no-launch-profile --urls http://127.0.0.1:5193
```

Open the local development address in a browser. Routes are `/` for the material
dashboard, `/transactions` for entry/history, and `/settings` for projects and backups.
For a new browser profile, create the first project in settings; creation selects
it automatically. Define a material, then record inflow and usage to inspect stock.

For browser checks, run in a second terminal:

```powershell
node tests/browser-checks.cjs "$env:TEMP\sitepack-browser-checks\node_modules\playwright"
```

Playwright was installed in a temporary directory during verification, not as an
application dependency. If unavailable, use the setup command in `UI_REVIEW.md`.
The script requires installed Microsoft Edge and uses an isolated browser context.
Restart the development server after rebuilding so its static-asset mappings match
the new build; stale mappings caused a startup timeout during an earlier check.

For published PWA checks, no separate development server is needed:

```powershell
dotnet publish -c Release -o tests/artifacts/pwa-publish --nologo
node tests/pwa-checks.cjs "$env:TEMP\sitepack-browser-checks\node_modules\playwright"
```

The PWA script starts and closes its own local static server and isolated browser
contexts. It serves the published output at `/` and `/Material_Tracker/`, emulating
the GitHub Pages base-href rewrite. Generated publish output is ignored by Git.

## Review artifacts

- [Complete UI file index and verification instructions](UI_REVIEW.md)
- [Service contracts and component integration](Services/README.md)
- [Desktop dashboard](tests/artifacts/dashboard-desktop.png)
- [Mobile dashboard](tests/artifacts/dashboard-mobile.png)
- [Desktop transaction form](tests/artifacts/transaction-desktop.png)
- [Mobile transaction form](tests/artifacts/transaction-mobile.png)

Screenshots are generated files ignored by Git and may not exist in a fresh
checkout. Rerunning browser checks regenerates them. Mobile checks used a 390px
viewport; screenshots were inspected and page-level horizontal overflow was checked.

## Implemented

| File | Responsibility |
| --- | --- |
| `Models/Project.cs` | Project ID, name, and UTC creation timestamp. |
| `Models/Material.cs` | Project-owned material, category, unit, and optional stock warning threshold. |
| `Models/MaterialEntry.cs` | Contains `MaterialTransaction`, the class name specified by SPEC; handles both inflow and outflow. |
| `Models/TransactionType.cs` | `Inflow = 0`, `Outflow = 1`. |
| `Models/SitePackData.cs` | Schema version, project/material/transaction collections, and nullable active project ID. |
| `Services/IStorageService.cs` | Browser persistence boundary: `LoadAsync` and `SaveAsync`. |
| `Services/LocalStorageService.cs` | Reads/writes validated JSON using built-in JS interop calls to localStorage. |
| `Services/DataValidator.cs` | Validates schema, IDs, references, unique names, predefined units, quantities, dates, timestamps, phone text, and usage fields. |
| `Services/DataJson.cs` | Internal JSON codec; checks required fields and duplicate property names and creates detached copies. |
| `Services/SitePackDataException.cs` | Exceptions with Persian messages suitable for the future UI. |
| `Services/AppState.cs` | Initialization, serialized updates, detached snapshots, persisted project selection, active-project queries, and change/error notifications. |
| `Services/IJsonBackupService.cs` | JSON export/restore contract. |
| `Services/JsonBackupService.cs` | Complete JSON backup strings and validated restore with a Persian browser confirmation dialog. |
| `Services/README.md` | Service usage, integration guidance, and limitations. |
| `tests/SitePack.StorageChecks/` | Dependency-free console checks using production source files and simulated JS interop. |
| `Services/ProjectService.cs` | Project creation/selection/rename and safe deletion, with `IProjectService`. |
| `Services/MaterialService.cs` | Project-scoped management, search/filtering, computed stock, and unit/history protection, with `IMaterialService`. |
| `Services/TransactionService.cs` | Scoped creation/edit/deletion and history filters, with `ITransactionService`. |
| `Utilities/PersianDate.cs`, `Utilities/PersianText.cs` | Jalali conversion/validation and Persian number formatting/parsing. |
| `Components/` | State subscription, project selector, material form/table, transaction form, Jalali date input, and stock badges. |
| `Pages/Home.razor`, `Pages/Transactions.razor`, `Pages/Settings.razor` | Dashboard, transaction history/forms, project management, and JSON file backup/restore. |
| `Layout/`, `wwwroot/css/app.css`, `wwwroot/index.html` | Responsive Persian RTL shell, navigation, and localized startup/error text. |
| `wwwroot/fonts/` | Bundled Vazirmatn variable font and its OFL license. |
| `wwwroot/js/sitepack.js` | Browser-local today value and JSON download helper. |
| `tests/browser-checks.cjs` | Real-browser workflows in headless Edge; screenshots go to ignored `tests/artifacts/`. |
| `wwwroot/manifest.webmanifest` | Persian RTL standalone manifest, relative start URL/scope, and blue theme. |
| `wwwroot/icon-192.png`, `wwwroot/icon-512.png` | Actual PNG box icons at the manifest sizes, with room for maskable cropping. |
| `wwwroot/service-worker.js` | Versioned static precaching, scoped cache cleanup, `clients.claim()`, and cached app-shell navigation. |
| `tests/pwa-checks.cjs` | Published PWA checks for both base paths, offline deep-route reload, font loading, and persisted online/offline writes. |

`Program.cs` registers storage, state, backup, project, material, and transaction
services as scoped services. `Material_Tracker.csproj` excludes the console checks from the
Blazor compilation/content. No additional NuGet packages were introduced.

## PWA behavior and deployment

- `Material_Tracker.csproj` enables the SDK-generated `service-worker-assets.js`
  manifest and uses the same worker source for build and publish. The asset list
  includes the Blazor runtime so offline startup does not depend on previously
  visiting every page or downloading runtime files on demand.
- `wwwroot/index.html` links the manifest and Apple touch icon, sets theme color
  to `#0284c7`, and registers the worker after `Blazor.start()` resolves. Worker
  URLs and scope are resolved against `document.baseURI`; asset paths are relative.
- Preserve the SDK's `_framework/blazor.webassembly#[.{fingerprint}].js` path
  syntax in source HTML so fingerprint substitution continues to work.
- Caches are named by application base path and generated asset version. Install
  precaches the asset list; activation removes older caches for that base path
  and claims clients. Updates wait for existing app tabs to close; there is no
  forced `skipWaiting()` that could mix running application versions.
- Fetch handling serves same-origin, in-scope navigations from cached
  `index.html` and known static assets from the active cache. Other requests
  pass through. The worker does not modify localStorage or user backup data.
- `.github/workflows/deploy.yml` already rewrites the published base href to
  `/Material_Tracker/` and copies the result to `404.html`. The worker excludes
  `404.html` and its own scripts from precaching. It omits integrity validation
  for `index.html` because that deployment rewrite changes the generated hash;
  other precached assets retain SDK integrity hashes.
- Offline startup requires an initial successful online worker installation.
  Use HTTPS for deployment or localhost for development. Physical-device install
  flows, Apple home-screen behavior, and multi-version update transitions still
  need manual verification.

## Data and state contract

- All persisted data is one JSON document under `sitepack:data:v1`; schema version is `1`.
- Missing storage initializes and persists an empty valid document. Corrupt existing data raises an error and is preserved.
- Transaction dates are Gregorian `DateOnly` values, serialized as ISO dates. Creation timestamps are UTC `DateTime` values. Phones remain strings.
- `MainLayout` initializes state after its first render, displays load errors, and leaves settings available for corrupt-data recovery. `StateComponent` manages page subscriptions and disposal.
- Use `UpdateAsync(candidate => { ... })` for changes. Operations are serialized within the state instance; validation and persistence precede publication of the new state.
- `Snapshot`, `ActiveMaterials`, and `ActiveTransactions` are detached copies. Editing those copies does not save changes. Do not retain candidates for later mutation or call state operations from an update callback.
- `SelectProjectAsync(Guid?)` persists selection. Active queries return empty results when no project is selected. Transactions belong to a project through their material.
- Subscribe to `Changed`, schedule rendering with `InvokeAsync(StateHasChanged)`, and unsubscribe on component disposal. Event handlers should not throw.
- Catch `SitePackDataException` for Persian feedback. `LastError` records errors from state operations; backup parsing can throw before entering state operations.
- Ordinary updates block deletion of materials with existing transactions, moving those materials between projects, and deletion of active or populated projects.
- `ExportJsonAsync()` returns a JSON string. `RestoreJsonAsync(json)` validates before confirmation, returns `false` on cancellation, and replaces state/storage only after a successful write. Confirmed restore can recover corrupt existing storage and deliberately bypasses ordinary deletion restrictions.
- Stock is not persisted as a separate counter. `MaterialService` calculates inflow minus outflow within the active project.

## Verification already completed

The PWA upgrade was verified with these results:

```powershell
dotnet build
# Build succeeded: 0 warnings, 0 errors.

dotnet run --project tests/SitePack.StorageChecks/SitePack.StorageChecks.csproj
# 50/50 checks passed.

dotnet publish -c Release -o tests/artifacts/pwa-publish --nologo
# Publish succeeded.

node tests/pwa-checks.cjs "$env:TEMP\sitepack-browser-checks\node_modules\playwright"
# PASS / and /Material_Tracker/: manifest, PNG sizes, worker scope,
# offline deep-route startup, bundled font, persistence, and offline writes.
```

Checks cover initialization, reload/backup round trips, phone/date/decimal
preservation, project isolation, concurrent updates, detached state, failed
reads/writes, corrupt-data recovery, confirmation/cancellation, deletion guards,
and invalid backup rejection, plus Jalali leap dates, Persian decimal parsing,
business-service ownership rules, stock changes, and stable timestamps.
During the earlier UI verification, `tests/browser-checks.cjs` passed 11 workflow
groups in headless Edge, including
mobile overflow, offline in-session writes, transaction edits/filters, failed-write
selector recovery, backup download/restore, and recovery from corrupt localStorage.
Desktop/mobile dashboard and transaction-form screenshots were visually inspected.
The new PWA checks use separate browser contexts for each base path, preserve
online-created records across offline reloads, create another project offline,
and confirm that both projects survive a second offline reload. These checks
cover published browser behavior; they do not automate native app installation.

## Remaining work and recommended sequence

1. Complete the broader export/import responsibility from SPEC when requested. JSON service logic and file controls exist; the full `IExportImportService` and CSV exports are not implemented. CSV needs UTF-8, Jalali dates, proper escaping, and spreadsheet formula protection.
2. Verify native installation on physical Android/iOS devices and desktop, plus update activation after deploying a new asset version. Published offline startup already passes at both base paths.
3. Expand cross-browser, accessibility, and mobile-device verification as needed. Current browser coverage uses headless Edge, not physical phone keyboards or native dialogs on every platform.

Build after each code step and resolve warnings/errors before reporting completion.

## Limits and constraints to preserve

- No backend, EF Core server components, cloud synchronization, heavy component libraries, or external API calls for core data features.
- The template dashboard/navigation have been replaced, and Counter/Weather routes removed. The UI uses Persian RTL, bundled Vazirmatn, Persian date/number displays, and numeric telephone inputs.
- Settings provides JSON download/upload, a 10 MiB upload limit, validation, and confirmation before replacement. CSV controls are not implemented.
- LocalStorage is quota-limited and browser-local; clearing it removes data. User-managed backups remain necessary.
- State synchronization is limited to one tab. Other tabs are not refreshed automatically, and concurrent tab writes can overwrite each other.
- Persistence requires no network for data operations. PWA static caching supports offline startup after successful initial installation; it does not provide cloud backup or cross-device synchronization.
- Future UI mutations should use the scoped business services; preserve their ownership, history, and confirmation rules.
- Preserve existing user changes and staged files. No commit or deployment was created during this work.
- The repository already contained staged files before the UI work. Do not treat the working tree as clean or stage/reset unrelated files when continuing.
