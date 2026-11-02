# SitePack | سایت‌پَک

SitePack is a Persian construction-material tracker built with Blazor WebAssembly.
It helps site supervisors manage deliveries, record consumption, and monitor
material stock separately for each project. Data is stored in the browser, without
a backend or cloud account.

The core data layer, persistence services, initial UI, and installable PWA with
offline startup are implemented. CSV export remains planned work.

## Features

- Multiple projects with persisted active-project selection and isolated material/transaction views.
- Material creation and editing, predefined construction units, search, and category filters.
- Delivery and consumption records with quantities, dates, document numbers, notes, and work areas.
- Seller and driver details, with phone numbers preserved as strings and numeric telephone inputs.
- Stock calculated from deliveries minus usage, with low-stock and negative-stock indicators.
- Persian RTL interface, Persian number displays, Jalali date entry, and an «امروز» button.
- Bundled Vazirmatn font, responsive layouts, and large touch targets.
- Versioned JSON backup download and validated restore with confirmation.
- Protection against deleting materials with history or active/populated projects.
- Installable PWA with Persian app metadata, blue box icons, and offline startup after initial caching.

## Requirements

- .NET 10 SDK: the application targets `net10.0`.
- A modern browser with JavaScript and localStorage enabled.
- Network access for the initial NuGet package restore.

Node.js, Playwright, and Microsoft Edge are needed only for the optional browser
checks; they are not application dependencies.

## Run locally

From the repository root:

```powershell
dotnet restore
dotnet build
dotnet run --no-build --no-launch-profile --urls http://127.0.0.1:5193
```

Open `http://127.0.0.1:5193` in your browser. The local development server serves
the Blazor application; application records remain in browser storage.

Restart the server after rebuilding so its static-asset mappings match the build.
The SDK and installed project packages must support the versions in
`Material_Tracker.csproj`.

## First use

1. Open **پروژه‌ها و پشتیبان** and create a project. The new project becomes active.
2. Open **موجودی مصالح** and define a material, its category, unit, and optional warning threshold.
3. Open **ورود و مصرف** to record a delivery or consumption. Consumption requires a work area.
4. Review stock on the dashboard. Switch projects using the selector at the top.
5. Regularly download a JSON backup from settings and keep it outside the browser.

| Route | Page |
| --- | --- |
| `/` | Material dashboard and management |
| `/transactions` | Delivery/consumption forms and transaction history |
| `/settings` | Project management and JSON backup/restore |

## Storage and backups

All application data is stored as one versioned JSON document under
`sitepack:data:v1`, accessed through `IStorageService`. Dates are stored as Gregorian
ISO values and displayed/entered in the Jalali calendar. Creation timestamps use
UTC; telephone values remain strings to preserve leading zeroes and `+`.

Missing storage initializes an empty document. Invalid existing data is preserved
and reported instead of being silently overwritten. Restore validates the backup
before asking for confirmation and replaces all projects, materials, transactions,
and the active selection only after a successful save. The upload limit is 10 MiB.

Browser storage has capacity limits. Clearing site data removes records, and data
does not synchronize between browsers, devices, or open tabs. Different origins,
including different local ports, have separate storage. Use JSON backups to move
data or recover it.

## Install and use offline

1. Open SitePack online over HTTPS (or localhost for development) and allow the initial app load and service-worker installation to finish.
2. Use your browser's install action or **Add to Home Screen**, where supported.
3. Reopen the app offline to manage projects, materials, and transactions. Page reloads and direct page routes also work after caching completes.

The worker caches the app shell, Blazor runtime, styles, icons, and bundled font.
Records remain in localStorage independently of the static cache. New app versions
wait for existing SitePack tabs and app windows to close before activating; reopen
the app to use the installed update. Continue keeping JSON backups: clearing site
data removes local records as well as cached assets.

Published offline startup has been checked in headless Edge. Native installation
on physical devices, Apple home-screen behavior, and update transitions still need
manual verification.

## Publish and GitHub Pages

The app is static and requires no backend. Publish it with:

```powershell
dotnet publish -c Release -o tests/artifacts/pwa-publish --nologo
```

Serve the generated `wwwroot` directory over HTTPS, including the generated
`service-worker-assets.js`. Configure the published `<base href>` for the hosting
path and serve `index.html` as the fallback for app routes.

The existing [deployment workflow](.github/workflows/deploy.yml) runs on pushes to
`main` or manual dispatch. It publishes the app, sets the base href to
`/Material_Tracker/`, copies `index.html` to `404.html` for page routing, adds
`.nojekyll`, and deploys to the `gh-pages` branch. Configure GitHub Pages to serve
that branch from its root. Manifest and worker paths resolve against the app base;
static caches are versioned separately for each base path.

## Verification

```powershell
dotnet build
dotnet run --project tests/SitePack.StorageChecks/SitePack.StorageChecks.csproj
```

The PWA upgrade verification on **November 2, 2026** passed with **zero build
warnings/errors**, a successful Release publish, and **50/50 console checks**.
Checks cover persistence, validation, recovery,
project isolation, state updates, Jalali dates, Persian decimal parsing, and
business-service rules. The console harness uses production source files and
simulates browser interop without additional test packages.

For the optional browser checks, install Playwright outside the app dependencies
and use an installed Microsoft Edge browser:

```powershell
npm.cmd install --prefix "$env:TEMP\sitepack-browser-checks" --no-save --package-lock=false --registry=https://registry.npmjs.org playwright
dotnet publish -c Release -o tests/artifacts/pwa-publish --nologo
node tests/pwa-checks.cjs "$env:TEMP\sitepack-browser-checks\node_modules\playwright"
```

`tests/pwa-checks.cjs` passed at both `/` and `/Material_Tracker/`, covering manifest
metadata, PNG dimensions, worker scope, offline deep-route startup, the local font,
preserved records, and offline writes across reloads. It starts and closes its own
static server and isolated browser contexts; no separate server is needed.

During the earlier UI verification, `tests/browser-checks.cjs` passed **11 workflow
groups** in headless Edge, covering the UI, mobile overflow, offline in-session writes,
backup/restore, and failed storage operations. See [UI_REVIEW.md](UI_REVIEW.md) for
tooling setup and commands. Screenshots are generated in `tests/artifacts/`, which
is ignored by Git and may be absent from a fresh checkout.

## Project structure

```text
Components/   Reusable forms, project selector, table, date input, and stock badges
Layout/       Shared application shell and navigation
Models/       Projects, materials, transactions, and the versioned data document
Pages/        Dashboard, transactions, settings, and missing-page UI
Services/     Storage, shared state, business logic, validation, and JSON backups
Utilities/    Persian calendar conversion and number formatting/parsing
wwwroot/      App shell, styles, bundled font, browser helpers, PWA manifest/icons/worker
tests/        Console verification, UI workflows, and published PWA checks
```

`Models/MaterialEntry.cs` contains the SPEC-named `MaterialTransaction` class.
Stock is computed from transactions rather than stored as a separate counter.

## Documentation

- [PRD.md](PRD.md): product requirements and MVP scope.
- [SPEC.md](SPEC.md): architecture, data models, and service contracts.
- [AGENTS.md](AGENTS.md): implementation and verification rules for agents.
- [HANDOFF.md](HANDOFF.md): current implementation status and remaining work.
- [UI_REVIEW.md](UI_REVIEW.md): UI file index, review guidance, and browser checks.
- [Services/README.md](Services/README.md): state/persistence integration details.

Read the product and technical specifications before changing implementation.
Keep changes incremental, preserve the Persian RTL experience and browser-only
persistence, and run `dotnet build` after code changes.

## Remaining work

- CSV exports with Jalali dates, correct escaping, and spreadsheet formula protection.
- Native installation on desktop and physical Android/iOS devices, plus update activation after a new deployment.
- Broader browser, accessibility, and physical mobile-device testing.

The bundled Vazirmatn font is accompanied by its [OFL license](wwwroot/fonts/OFL.txt).
