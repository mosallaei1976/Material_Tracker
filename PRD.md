# Product Requirements Document (PRD)

## Project Name
**Material Tracker** (Construction & Renovation Site Material Logger)

---

## 1. Problem Statement
Job sites and renovation projects often suffer from disorganized material deliveries, lost paper receipts, and lack of real-time visibility into current stock (e.g., cement bags, tiles, rebar, paint). High-end ERP software is overly complex, requires steady internet connectivity, and is impractical for fast on-site entry by architects or site engineers.

---

## 2. Product Goal
Build a lightweight, offline-first Web application (PWA) using Blazor WebAssembly that allows architects, project managers, and supervisors to track deliveries, log daily consumption, and inspect inventory balances directly from mobile or desktop browsers.

---

## 3. Target Audience
- Architects overseeing construction and renovation sites
- Site supervisors and field engineers
- Small-scale contractors and renovation crews

---

## 4. MVP Scope (Phase 1)

### 4.1. Core Features
1. **Multi-Project Support (Project Tagging):**
   - Create and switch between distinct projects/job sites (e.g., "Villa Damavand", "Qazvin Renovation").
   - Filter all material data, deliveries, and usages strictly by active project.

2. **Material Definition & Standard Units:**
   - Define materials with attributes: Name, Project ID, Category (e.g., Structural/Concrete, Plumbing, Finishing, Electrical).
   - Pre-defined construction measurement units dropdown to prevent typos: Bags (کیسه), Tons (تن), Meters (متر طول), Sqm (متر مربع), Pieces/Bars (شاخه / عدد), Packs (بسته / کارتن).

3. **Delivery / Inflow Logging:**
   - Record inbound entries: Material, Quantity, Supplier, Delivery Date (Jalali), Delivery Note / Bill Number, Notes.

4. **Usage / Outflow Logging:**
   - Record consumed quantities: Material, Quantity, Date (Jalali), Specific Work Area or Purpose (e.g., "2nd Floor Bathroom Plastering").

5. **Current Stock Dashboard & Search:**
   - Real-time stock balance calculated as:
     $$\text{Current Stock} = \sum \text{Deliveries} - \sum \text{Usages}$$
   - Warning indicator for low stock or negative balances.
   - Quick search input and category filter for fast on-site lookup.

6. **Data Safety (Backup, Restore & Export):**
   - Export current stock and transaction history to CSV/Excel format for client reports.
   - One-click JSON backup export and restore to ensure data portability across devices and prevent browser cache loss.

7. **Offline-First Persistence:**
   - All data saved locally in the browser (LocalStorage / IndexedDB) without requiring an active server connection.

### 4.2. Out of Scope for Phase 1 (Future Work)
- Multi-user remote synchronization via cloud API.
- Purchase order and automated payment/cost tracking.
- Photo attachment capturing for physical receipts (planned for Phase 2).

---

## 5. Localization & UI Requirements (Persian / RTL)
- **Language & Direction:** The entire user interface must be in Persian (Farsi) with strict RTL (Right-to-Left) layout support (`dir="rtl"`).
- **Typography:** Use the standard **Vazirmatn** font across the entire application for clear readability on mobile screens.
- **Calendar & Dates:** All dates (input, display, reports) must use the **Persian (Jalali) Calendar** (شمسی).

---

## 6. Non-Functional Requirements
- **Mobile-First UX:** Large touch targets, minimal typing, optimized for quick thumb entry on mobile devices on-site.
- **High-Contrast / Outdoor Readability:** Clean, high-contrast visual design ensuring readability under bright daylight and dusty job site environments.
- **Performance:** Instant load time; zero network latency for CRUD operations.
- **Reliability:** 100% functional offline without internet access.
