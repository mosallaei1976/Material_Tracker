# SitePack — Technical Specification

## 1. Product and Scope

SitePack (سایت‌پَک) is a Persian, offline-first PWA for tracking construction materials across multiple projects.

This specification covers the Phase 1 MVP described in `PRD.md`. The application runs as Blazor WebAssembly in the browser. It has no backend, user accounts, or cloud synchronization in Phase 1.

## 2. Architecture

- **Frontend:** Blazor WebAssembly.
- **Persistence:** Browser `localStorage`, accessed through a C# service using JavaScript interop.
- **Application logic:** C# services; Razor components handle presentation and user interaction.
- **Offline operation:** PWA static assets are cached for use after the application has been loaded successfully. Data reads and writes do not require a network connection.
- **Dependencies:** Prefer built-in .NET and browser capabilities. Add third-party packages only when they solve a demonstrated requirement.

`localStorage` has browser-dependent capacity limits. It is suitable for the text-based MVP, but it is not a substitute for user-managed backups. Do not promise unlimited records or data survival after browser storage is cleared.

## 3. Data Models

### Project

| Field | Type | Rules |
|---|---|---|
| `Id` | `Guid` | Unique identifier |
| `Name` | `string` | Required; unique within the app |
| `CreatedAtUtc` | `DateTime` | Creation timestamp |

### Material

| Field | Type | Rules |
|---|---|---|
| `Id` | `Guid` | Unique identifier |
| `ProjectId` | `Guid` | Must reference an existing project |
| `Name` | `string` | Required; unique within its project |
| `Category` | `string` | Required |
| `Unit` | `string` | Required; selected from predefined units |
| `MinStockWarning` | `decimal?` | Optional; cannot be negative |

Initial unit choices: عدد، شاخه، کیسه، بسته، کارتن، کیلوگرم، تن، متر طول، متر مربع، متر مکعب، لیتر.

### MaterialTransaction

Use one transaction model for both deliveries and usage.

| Field | Type | Rules |
|---|---|---|
| `Id` | `Guid` | Unique identifier |
| `MaterialId` | `Guid` | Must reference an existing material |
| `Type` | `TransactionType` | `Inflow` or `Outflow` |
| `Quantity` | `decimal` | Required; greater than zero |
| `Date` | `DateOnly` | Stored as a Gregorian calendar date |
| `SellerName` | `string?` | Optional; delivery only |
| `SellerPhone` | `string?` | Optional; delivery only |
| `DriverName` | `string?` | Optional; delivery only |
| `DriverPhone` | `string?` | Optional; delivery only |
| `DocumentNumber` | `string?` | Optional; delivery note or bill number |
| `WorkArea` | `string?` | Required for usage; e.g., طبقه دوم |
| `Purpose` | `string?` | Optional; usage description |
| `Notes` | `string?` | Optional |
| `CreatedAtUtc` | `DateTime` | Creation timestamp; also used for stable sorting |

Phone numbers are **strings, not numbers**: they can start with `0` or `+` and must not lose those characters. For a mobile-friendly keyboard, use an appropriate telephone input with `inputmode="numeric"` where practical. Validate the entered text without converting it to a numeric type.

A transaction belongs to a project through its material's `ProjectId`. Every transaction query must respect the active project.

## 4. Storage Design

Define `IStorageService` as the browser-storage boundary. Store a versioned JSON document under one application-specific key, such as `sitepack:data:v1`.

The stored document contains:

- `SchemaVersion`
- `Projects`
- `Materials`
- `Transactions`
- `ActiveProjectId`

The service must:

1. Initialize a valid empty document when no data exists.
2. Serialize and deserialize the document consistently.
3. Save changes after successful validation.
4. Report storage, parsing, or quota errors to the UI; never silently discard data.
5. Keep storage implementation details out of Razor pages.

Do not use `localStorage` to store photos in Phase 1.

## 5. Application Services

### `IProjectService`

- Create and list projects.
- Select the active project and persist that selection.
- Rename a project.
- Prevent deletion of a project that still contains materials or transactions unless an explicit, separately designed deletion flow is added.

### `IMaterialService`

- Create, list, and edit materials within the active project.
- Search by name and filter by category.
- Prevent deletion of a material that has transactions, so history remains intact.
- Calculate stock per material:

  `CurrentStock = Sum(Inflow.Quantity) - Sum(Outflow.Quantity)`

- Flag stock below the material's optional warning threshold.
- Display negative stock as a visible warning rather than hiding or silently correcting it.

### `ITransactionService`

- Record inflow and outflow transactions.
- Validate material ownership against the active project.
- List transactions by date, type, and material.
- Support editing and deletion with user confirmation.

### `IExportImportService`

- Export a complete, versioned JSON backup.
- Validate a JSON backup before importing it, including schema version, IDs, references, quantities, and dates.
- Show a confirmation before replacing existing data during restore.
- Export stock and transaction history as separate UTF-8 CSV files that open in Excel.
- Export user-facing dates in Jalali format. Escape CSV fields correctly and prevent spreadsheet formula interpretation of user-entered text.

## 6. Persian Dates

- **UI:** All date entry and display use the Persian (Jalali) calendar.
- **Storage:** Save calendar dates as Gregorian ISO date values (`yyyy-MM-dd`), without a time or timezone.
- **Conversion:** Centralize Jalali/Gregorian conversion in a date utility using .NET calendar support.
- **Validation:** Reject invalid Jalali dates before saving.
- **Today button:** Every date-entry form has an «امروز» button. It sets the field to today's date in the user's local timezone and displays that date in Jalali form.

Do not derive a transaction's calendar date by converting a UTC timestamp; that can shift the visible day.

## 7. UI and Components

The entire UI is Persian and RTL. Apply `dir="rtl"` at the application layout level and use the **Vazirmatn** font, bundled with the app so it remains available offline.

Suggested page responsibilities:

- **Dashboard:** Active-project selector, stock list, low-stock/negative-stock warnings, search, and category filter.
- **Transactions:** Inflow/outflow history and entry forms.
- **Settings:** Project management, JSON backup/restore, and CSV exports.

Suggested reusable components:

- `ProjectSelector`
- `MaterialForm`
- `TransactionForm`
- `JalaliDateInput`
- `StockStatusBadge`

Form labels, validation errors, confirmations, and empty states must be in Persian. Prioritize large touch targets and high-contrast readability at job sites. Show an empty-state prompt to create the first project when no project exists.

## 8. Branding and PWA

- **Display name:** سایت‌پَک
- **English name:** SitePack
- **Short description:** مدیریت مصالح پروژه
- **Logo direction:** A simple, recognizable material box/cube with a subtle check mark; charcoal and safety amber are the proposed colors.
- **PWA assets:** Provide actual icon files in the sizes required by the app manifest. An SVG file cannot become a PNG icon merely by changing its filename.

## 9. MVP Verification

The implementation is complete when these checks pass:

1. A user can create two projects; each project shows only its own materials and transactions.
2. Inflow increases stock, outflow decreases it, and warnings reflect the calculated balance.
3. Seller and driver phone numbers retain leading zeroes and are easy to enter on a phone.
4. Jalali date entry, display, invalid-date handling, and the «امروز» button work correctly.
5. Data remains available after a page reload and during an offline session after the PWA has been loaded.
6. A JSON backup restores the same projects, materials, and transactions after validation and confirmation.
