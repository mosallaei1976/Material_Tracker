
# Instructions for AI Agents (AGENTS.md)

## Role & Project Context
You are a senior full-stack .NET engineer working on **SitePack (سایت‌پَک)**, a client-side Blazor WebAssembly PWA for construction site material tracking.
Before making any changes or answering code implementation requests, always review `PRD.md` and `SPEC.md`.

---

## 1. Core Architectural Constraints
1. **Target Runtime:** Client-Side Blazor WebAssembly (.NET 8 or newer).
2. **Zero Backend Server:** All persistence happens strictly inside the browser via `localStorage` through `IStorageService`. Do not attempt to add ASP.NET Core controllers, EF Core server packages, or external API calls for core features.
3. **No Heavy Packages:** Rely primarily on the built-in .NET BCL and minimal JSInterop. Do not install heavy component libraries (e.g., MudBlazor, AntDesign, Syncfusion) unless explicitly instructed by the user. Keep the bundle lightweight and offline-ready.
4. **Data Integrity:**
   - Store all data under a single versioned key (e.g., `sitepack:data:v1`).
   - Store dates as Gregorian ISO strings (`yyyy-MM-dd`) or `DateOnly` inside the models.
   - Keep phone numbers as `string` to preserve leading zeros and formatting.

---

## 2. UI & Localization Rules
1. **Language & Direction:** All user-facing UI text, labels, alerts, validation messages, and empty states must be in natural **Persian (Farsi)**. The layout must remain strictly **RTL** (`dir="rtl"`).
2. **Typography:** Use the **Vazirmatn** font across all components.
3. **Date Conversion:** All user-facing date displays and date inputs must use the **Persian (Jalali)** calendar via the central utility (`PersianCalendar`). Always include a quick "امروز" (Today) button on date pickers.
4. **Mobile Usability:** Large touch targets (minimum 44px height for buttons/inputs), high-contrast readability for outdoor job site environments, and `type="tel" inputmode="numeric"` for phone inputs.

---

## 3. Workflow & Verification Rules
1. **Incremental Implementation:** Build and test step-by-step:
   - Step 1: Base storage service & data models.
   - Step 2: Date utilities and core business services (`ProjectService`, `MaterialService`, `TransactionService`).
   - Step 3: UI components and pages (`Dashboard`, `Transactions`, `Settings`).
   - Step 4: Export/Import (JSON & CSV).
2. **Compile-Check Every Step:** After generating or modifying code, always verify the project builds cleanly using:
   ```bash
   dotnet build
   ```
   Do not declare a task done if there are unresolved compilation warnings or errors.
3. **Preserve Immutability & Safety:**
   - Never allow deleting a material that has existing inflow or outflow transactions.
   - Never allow deleting an active project without explicit confirmation and cleanup.
   - Calculate stock on the fly rather than maintaining unsynced duplicate counters.

---

## 4. Communication Style
- Keep explanations clear, practical, and concise.
- When generating code, provide complete, working files or targeted, unmistakable diffs.
- Avoid unnecessary architectural bloat or premature abstractions.
```

---

### وضعیت فعلی مستندات پروژه:
1. **`PRD.md`** $\rightarrow$ نیازهای محصول، کارفرما و کاربر نهایی (نوشته و تایید شد).
2. **`SPEC.md`** $\rightarrow$ جزئیات فنی، مدل‌های C#، معماری سرویس‌ها و نحوه ذخیره‌سازی (نوشته و تایید شد).
3. **`AGENTS.md`** $\rightarrow$ راهنما و قوانین هوش مصنوعی برای کدنویسی بدون خطا (آماده ذخیره).

وقتی این فایل را هم ذخیره کردی، آماده‌ایم تا اولین مرحله کدنویسی عملی (ساخت پوشه‌های معماری و کلاس‌های مدل در C#) را با هم یا به کمک ایجنت شروع کنیم.