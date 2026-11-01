# SitePack UI review

Implemented November 2, 2026. Ready for user review.

## Updated files

| File | Review focus |
| --- | --- |
| `Pages/Home.razor` | Material dashboard, summary counts, search/category filters, first-project/material prompts, and management actions. |
| `Pages/Transactions.razor` | Inflow/outflow entry, history, material/type/Jalali date filters, details, editing/deletion confirmation, and stock feedback. |
| `Pages/Settings.razor` | Project creation/selection/rename, safe deletion, JSON file download/upload, restore cancellation, and corrupt-storage recovery. |
| `Pages/NotFound.razor` | Persian missing-page message and navigation. |
| `Components/ProjectSelector.razor` | Persisted active selection, busy/error states, and visual rollback when saving fails. |
| `Components/MaterialForm.razor` | Name/category/unit/optional stock threshold with Persian validation and decimal input. |
| `Components/MaterialTable.razor` | Computed balances, units, warning thresholds, badges, edit/delete actions, and accessible horizontal scrolling. |
| `Components/TransactionForm.razor` | Conditional delivery/usage fields, decimal quantity, seller/driver strings, telephone keyboards, notes, and edit confirmation. |
| `Components/JalaliDateInput.razor` | Persian date text, Persian/Arabic/Latin digit input, compact eight-digit date entry, and browser-local «امروز». |
| `Components/StockStatusBadge.razor` | Distinct Persian negative/low/zero/available stock states. |
| `Components/StateComponent.cs` | Shared state subscriptions, rendering, and cleanup. |
| `Layout/MainLayout.razor` | RTL shell, startup, load-error/recovery presentation, and shared project selector. |
| `Layout/NavMenu.razor` | Persian navigation with active-route indication. |
| `Layout/MainLayout.razor.css`, `Layout/NavMenu.razor.css` | Old template rules replaced with pointers to shared styles. |
| `wwwroot/css/app.css` | Charcoal/amber styling, local font, responsive layout, visible focus, 44px touch targets, and isolated negative stock numbers. |
| `wwwroot/index.html` | Persian document language/direction, local font preload, localized loading/errors, and browser helper loading. |
| `wwwroot/fonts/Vazirmatn.woff2`, `wwwroot/fonts/OFL.txt` | Locally bundled variable font and license, from the official Vazirmatn repository. |
| `wwwroot/js/sitepack.js` | Local calendar day and Blob-based JSON downloads; no backend calls. |
| `Utilities/PersianDate.cs` | Central `PersianCalendar` conversion and invalid-date rejection. |
| `Utilities/PersianText.cs` | Persian display digits and culture-independent decimal parsing, including Persian/Arabic input digits. |
| `Services/ProjectService.cs` | Project operations and deletion protections; includes `IProjectService`. |
| `Services/MaterialService.cs` | Scoped material operations, calculated stock, search, and history/unit safeguards; includes `IMaterialService` and `MaterialStock`. |
| `Services/TransactionService.cs` | Ownership checks, conditional fields, stable edit timestamps, and history filtering; includes `ITransactionService`. |
| `Program.cs`, `_Imports.razor` | Scoped registrations and shared Razor namespaces. |
| `tests/SitePack.StorageChecks/Program.cs`, `tests/SitePack.StorageChecks/SitePack.StorageChecks.csproj` | Eight new service/date checks and linked utilities; original 42 checks retained. |
| `tests/browser-checks.cjs`, `.gitignore` | Headless Edge workflows; generated screenshots excluded from Git. |
| `HANDOFF.md`, `Services/README.md` | Updated implementation and integration guidance. |

Removed `Pages/Counter.razor` and `Pages/Weather.razor` template routes. Core data
models and storage schema/key remain unchanged. No application package was added.

## Verification

```powershell
dotnet build
# 0 warnings, 0 errors
dotnet run --project tests/SitePack.StorageChecks/SitePack.StorageChecks.csproj
# 50/50 checks passed
```

The browser script passed 11 workflow groups in headless Edge: RTL/font loading;
project/material creation; Persian decimals/Jalali leap dates/phone preservation;
required usage area/Today/negative stock/deletion guards; search/unit safeguards;
history filters/edit confirmation/stable timestamps; failed selection saves;
project isolation/reloads/mobile overflow; offline in-session data entry; JSON
download/confirmed and cancelled restore; corrupt-storage recovery with no browser
exceptions.

To reproduce browser checks without adding app dependencies, install Playwright in
a temporary directory and use an installed Edge browser:

```powershell
npm.cmd install --prefix "$env:TEMP\sitepack-browser-checks" --no-save --package-lock=false --registry=https://registry.npmjs.org playwright
dotnet run --no-build --no-launch-profile --urls http://127.0.0.1:5193
# In a second terminal:
node tests/browser-checks.cjs "$env:TEMP\sitepack-browser-checks\node_modules\playwright"
```

Restart the development server after rebuilding so its static-asset mappings match
the new build. The script uses an isolated browser context, not personal browser
storage. Screenshots are saved under `tests/artifacts/`:

- `dashboard-desktop.png` and `dashboard-mobile.png`
- `transaction-desktop.png` and `transaction-mobile.png`

## Manual review

Create two projects, add materials with different units/thresholds, register an
inflow and outflow, and switch projects. Check the negative/low-stock badges,
search, date filters, transaction edits, and phone inputs on a physical phone.
Download a backup, cancel a restore once, then confirm it and reload the page.

All user-facing dates are Jalali, while stored dates remain Gregorian ISO values.
Phone strings keep leading zeroes and `+`. The browser's local day drives «امروز».
Negative stock is visible and allowed. Referential deletion protections remain.

CSV export and PWA asset caching/offline startup remain for subsequent phases.
LocalStorage is quota-limited; clearing it deletes local data. Multi-tab state is
not synchronized. Browser checks cover Edge and an already-loaded offline session,
not installed-PWA startup or every device's keyboard/dialog behavior.
