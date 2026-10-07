# Production Meeting — Project Guide

This is the reference document for the whole codebase: what it does, how it's built, every
table in the database, and where to look when you need to change something. `DEPLOYMENT.md`
(same folder) covers IIS/production setup — this file is about the app itself.

---

## 1. What this app is

**Production Meeting** is a weekly KPI scorecard for a vehicle production plant, modeled on
the **SQCDPSMO** framework: **S**afety, **Q**uality, **C**ost, **D**elivery, **P**roduction,
**S**ustainability, **M**orale, **O**thers. Each week, someone from a Plant/Shop (production
line) enters that week's value for every KPI assigned to their line, and the app shows
whether each one is Green/Amber/Red against its target, rolls values up into Month and
Year-to-date totals, and surfaces all of that on two dashboards and an exportable report.

It's a single ASP.NET Core MVC app — no separate frontend, no API layer for a SPA. Razor
views + Bootstrap + jQuery, server-rendered, deployed to IIS. Windows Authentication only —
there is no login page and no password anywhere in this app.

---

## 2. High-level architecture

| Layer | Technology |
|---|---|
| Web framework | ASP.NET Core 9 MVC (Controllers + Razor Views) |
| Data access | Entity Framework Core, Code-First, migrations |
| Database | SQL Server |
| Auth | Windows Authentication (`Microsoft.AspNetCore.Authentication.Negotiate`) |
| User/role storage | ASP.NET Core Identity (`IdentityCore` only — no passwords, no cookie login) |
| Frontend | Razor views, Bootstrap 5, Bootstrap Icons, jQuery, jQuery Validation, Toastr, Chart.js (Executive Dashboard only), DataTables (CDN, loaded but not required everywhere) |
| Excel export | ClosedXML |

Standard three-layer shape per feature:

```
Controller  →  Service (interface + implementation)  →  ApplicationDbContext (EF Core)
     ↓
  Razor View (+ a small amount of jQuery per view, no SPA framework)
```

Controllers are thin — they resolve the current user, call one service method, and return a
view or JSON. All business logic (validation rules, status/variance math, audit logging,
access scoping) lives in the `Services/` layer.

---

## 3. Authentication & Authorization

- **No login page.** The app uses Negotiate (NTLM/Kerberos) Windows Authentication. The
  browser answers the server's `401 WWW-Authenticate: Negotiate` challenge with the signed-in
  Windows identity automatically — there's nothing for a user to type.
- `Services/Authentication/WindowsUserClaimsTransformation.cs` runs on every request after
  Windows proves who the caller is (`DOMAIN\username`). It looks up the matching
  `ApplicationUser`; if none exists **and this is the very first user ever to sign in**, it
  auto-creates that user as **Admin**. Otherwise an unmatched Windows account is "authenticated
  but not provisioned" (see `PmClaimTypes.Provisioned`), and lands on the Access Denied page
  until an Admin adds them via User Management.
- **Roles** (`Helpers/Roles.cs`): `Admin` > `Manager` > `ProductionUser` > `Viewer`, strictly
  hierarchical. Each policy in `Helpers/PolicyNames.cs` allows that role **and everything
  above it**:
  - `RequireViewerOrAbove` — read access to Dashboards/Reports/Meeting history. Everyone.
  - `RequireProductionUserOrAbove` — can open the Entry grid and save KPI values, save Executive
    Dashboard remarks.
  - `RequireManagerOrAbove` — currently no action is gated at exactly this level, it exists for
    future use between ProductionUser and Admin.
  - `RequireAdmin` — KPI Master, Master Data, User Management: entire controllers.
  - `FallbackPolicy` in `Program.cs` requires *some* authenticated user on every action by
    default — only `AccountController` and `HomeController.Error` are `[AllowAnonymous]`.
- **Per-plant/line scoping**: `UserPlantLineAccess` (one row per grant) controls which
  Plant/Line combinations a non-Admin user can see. A row with `LineId = null` means "every
  line in this Plant." Admins bypass this table entirely — they always see everything.
  `Services/UserAccessService.cs` is the single place this rule is implemented; every other
  service that needs to scope data to a user goes through it.

---

## 4. Core domain model (how the pieces fit together)

```
Plant ──┬── ProductionLine ("Shop")
        │         │
        │         ├── KpiMaster  (one row per KPI, scoped to one Plant+Line)
        │         │        │
        │         │        └── belongs to one Indicator (S/Q/C/D/P/SUST/M/O) + one Unit
        │         │
        │         └── MeetingSession  (one row per Plant+Line+week)
        │                   │
        │                   └── KpiTransaction  (one row per KpiMaster, per MeetingSession)
        │                             │
        │                             └── KpiTransactionAudit  (field-level change history)
        │
        └── UserPlantLineAccess  (which users can see/edit this Plant[/Line])
```

**Weekly cadence.** A "week" always means Monday–Sunday. Every date that represents "which
week" gets normalized to that week's **Monday** via `PmDates.GetMondayOfWeek` before it's
compared, stored, or looked up — this is the single most important invariant in the app. If
you ever add a new place that stores or queries a week, route it through `PmDates`, don't
reinvent Monday-finding.

**Fiscal year is April → March.** FY26 means 1-Apr-2025 to 31-Mar-2026, named after the
calendar year it *ends* in. `PmDates.FiscalYearEndYear(date)` gives you that ending year for
any date. "Week 1" of the fiscal year is the Monday–Sunday week containing 1-April — **not**
1-January, which is what .NET's built-in `ISOWeek.GetWeekOfYear` would give you. See §11 for
why this needed its own careful logic.

**One KPI, one row per week.** `KpiMaster` defines *what* is measured (Description, Unit,
Indicator, whether lower-is-better, and the fixed yearly F26/F27 targets). `KpiTransaction` is
the *actual value entered* for that KPI in a specific week's `MeetingSession`. There is no
separate "Line" dimension inside a KPI transaction (the vehicle-model dropdown was removed from
the Entry grid; see §13).

**Month Cum. / YTD are calculated, not typed in.** When a week's value is saved:
- **Month Cum.** = previous week's Month Cum. + this week's value, **unless** the calendar
  month changed since the last entered week, in which case it resets to just this week's value.
- **YTD** = previous week's YTD + this week's value, **unless** the April–March fiscal year
  changed since the last entered week, in which case it resets to just this week's value.

This logic lives in `ProductionMeetingService.ComputeCumulativeValues` (server-side,
authoritative) and is mirrored in `Entry.cshtml`'s JavaScript (`recalcCumulative`) so the grid
can preview the numbers instantly as you type, before you even hit Save.

**F26 / F27 targets are fixed per year, set once in KPI Master**, not re-typed every week.
`KpiMaster.F26Value`/`F27Value` are the yearly defaults; the Entry grid pre-fills each week's
F26/F27 cells from them, but lets someone override just that one week (with a confirm popup,
since it's meant to be fixed). Whatever value ends up in the cell — default or override — is
what gets saved onto that week's `KpiTransaction`. The column *headers* ("F26"/"F27 L4 Target")
shift forward automatically every April — see §11.

**Status (Green/Amber/Red)** is computed by the one shared function
`Helpers/KpiStatusCalculator.Compute(lowerIsBetter, weekValue, target)`:
- No value entered yet → `"Pending"`.
- No target set → `"NoTarget"`.
- On the good side of target → `"Green"`.
- On the bad side, within 5% of target → `"Amber"`.
- On the bad side, more than 5% off → `"Red"`.

Both dashboards use this exact function so Green/Amber/Red never drifts between them.

**Stored-procedure-sourced KPIs.** A KPI can be flagged `IsSourcedFromStoredProcedure = true`
with a `StoredProcedureName`. When the Entry grid loads and that KPI has no manually-saved
value yet for the week, the app calls that procedure (`EXEC <proc> @PlantId=, @LineId=,
@WeekStart=`, returning one decimal) to pre-fill the Week value — still editable, and once
someone saves a value manually it's never overwritten by the procedure again. The procedures
themselves live only in SQL Server, not in this codebase (see §12).

---

## 5. Database schema — every table

All tables below are created via EF Core Code-First migrations (`Migrations/`). Column types
shown are the SQL Server types EF generates; `AuditableEntity` columns (`CreatedBy`,
`CreatedDate`, `ModifiedBy`, `ModifiedDate`, `IsActive`) are repeated on every table that
inherits it, abbreviated below as **"+ audit columns"**.

### Identity tables (from ASP.NET Core Identity, `IdentityDbContext<ApplicationUser>`)
`AspNetUsers` (extended with `FullName`, `Designation`, `IsActive` — see `ApplicationUser`),
`AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`,
`AspNetRoleClaims`. Standard Identity shape — no local passwords are ever set or checked.

### Plants
| Column | Type | Notes |
|---|---|---|
| PlantId | int PK | |
| Code | nvarchar | |
| Name | nvarchar | |
| + audit columns | | |

### Departments
| Column | Type |
|---|---|
| DepartmentId | int PK |
| Name | nvarchar |
| + audit columns | |

*(Exists in the schema but isn't referenced by `KpiMaster`/`MeetingSession` anywhere — it's a
standalone master list, currently unused by the rest of the app's logic.)*

### ProductionLines ("Shop" in the UI)
| Column | Type | Notes |
|---|---|---|
| LineId | int PK | |
| PlantId | int FK → Plants | `Restrict` delete |
| Code | nvarchar | |
| Name | nvarchar | |
| + audit columns | | |

Unique index: `(PlantId, Code)`.

### Shifts
| Column | Type |
|---|---|
| ShiftId | int PK |
| Name | nvarchar |
| StartTime | time, nullable |
| EndTime | time, nullable |
| + audit columns | |

*(Shift was originally part of the meeting flow; it's been dropped from day-to-day use —
`MeetingSession.ShiftId` is always `null` now — but the table/model still exists.)*

### ProductModels (vehicle model, e.g. "XUV300")
| Column | Type |
|---|---|
| ModelId | int PK |
| Name | nvarchar |
| + audit columns | |

*(Named `ProductModel` in code, not `Model`, to avoid clashing with ASP.NET MVC's own
`Model`/Identity naming. Still used by `KpiTransaction.ModelId` and the Master Data admin
screen, but the Entry grid's Model dropdown was removed — see §13 — so new transactions never
set it.)*

### Indicators (the SQCDPSMO categories)
| Column | Type |
|---|---|
| IndicatorId | int PK |
| Code | nvarchar (`S`,`Q`,`C`,`D`,`P`,`SUST`,`M`,`O`) |
| Name | nvarchar (Safety, Quality, Cost, Delivery, Production, Sustainability, Morale, Others) |
| + audit columns | |

### Units
| Column | Type |
|---|---|
| UnitId | int PK |
| Name | nvarchar (e.g. "Nos", "%", "Rs./Veh", "Ltrs/Veh") |
| + audit columns | |

### KpiMasters — the KPI definitions
| Column | Type | Notes |
|---|---|---|
| KpiId | int PK | |
| PlantId | int FK → Plants | `Restrict` |
| LineId | int FK → ProductionLines | `Restrict` |
| IndicatorId | int FK → Indicators | `Restrict` |
| UnitId | int FK → Units | `Restrict` |
| Description | nvarchar | the KPI's name, e.g. "Offline Rework RPT XUV3XO" |
| DisplayOrder | int | controls row order within an Indicator group |
| LowerIsBetter | bit | true = smaller is good (defects, cost, incidents) |
| F26Value | decimal(18,2), nullable | fixed yearly "prior/baseline" target |
| F27Value | decimal(18,2), nullable | fixed yearly "current target / L4" |
| IsSourcedFromStoredProcedure | bit, not null, default 0 | |
| StoredProcedureName | nvarchar, nullable | only meaningful when the flag above is true |
| + audit columns | | |

Unique index: `(PlantId, LineId, IndicatorId, Description)` — you can't have two KPIs with the
same description under the same Indicator on the same Plant+Line.

### MeetingSessions — one per Plant+Line+week
| Column | Type | Notes |
|---|---|---|
| SessionId | int PK | |
| PlantId | int FK → Plants | `Restrict` |
| LineId | int FK → ProductionLines | `Restrict` |
| ShiftId | int FK → Shifts, nullable | always null in current usage |
| MeetingDate | datetime2 | always the week's Monday — see §4 |
| ConductedByUserId | nvarchar FK → AspNetUsers | `Restrict` |
| Remarks | nvarchar, nullable | free text, editable from the Executive Dashboard |
| Status | int | enum: `0 = Draft`, `1 = Completed` |
| + audit columns | | |

Unique index: `(PlantId, LineId, MeetingDate)` — one session per Plant+Line+week, period.

### KpiTransactions — one KPI's value for one week
| Column | Type | Notes |
|---|---|---|
| TransactionId | int PK | |
| SessionId | int FK → MeetingSessions | `Cascade` delete |
| KpiId | int FK → KpiMasters | `Restrict` |
| ModelId | int FK → ProductModels, nullable | legacy — see §13, always null for new rows |
| F26Value | decimal(18,2), nullable | this week's F26 (default or override) |
| F27Value | decimal(18,2), nullable | this week's F27 (default or override) |
| WeekValue | decimal(18,2), nullable | the actual entered/auto-filled figure |
| MonthCumValue | decimal(18,2), nullable | server-calculated, see §4 |
| YtdValue | decimal(18,2), nullable | server-calculated, see §4 |
| Remarks | nvarchar, nullable | free text per KPI row |
| + audit columns | | |

Unique index: `(SessionId, KpiId, ModelId)`.

### KpiTransactionAudits — field-level change log
| Column | Type | Notes |
|---|---|---|
| AuditId | int PK | |
| TransactionId | int FK → KpiTransactions | `Cascade` delete |
| KpiId | int | denormalized for convenience |
| FieldName | nvarchar | e.g. `"WeekValue"`, `"F26_Value"`, `"Remarks"` |
| OldValue | nvarchar, nullable | |
| NewValue | nvarchar, nullable | |
| ActionType | nvarchar | `"INSERT"` or `"UPDATE"` |
| ChangedByUserId | nvarchar FK → AspNetUsers | `Restrict` |
| ChangedDate | datetime2 | |

Not an `AuditableEntity` — it has its own `ChangedDate`/`ChangedByUserId` instead, since it
*is* the audit trail for something else, not a thing that itself gets audited. Written by
`ProductionMeetingService.SaveEntryAsync` every time a field actually changes (decimals are
compared numerically, not as formatted strings, so `1` vs `1.00` never falsely logs as a
change).

### UserPlantLineAccesses — per-user Plant/Line grants
| Column | Type | Notes |
|---|---|---|
| AccessId | int PK | |
| UserId | nvarchar FK → AspNetUsers | `Cascade` delete |
| PlantId | int FK → Plants | `Restrict` |
| LineId | int FK → ProductionLines, nullable | null = "every line in this Plant" |

Unique index: `(UserId, PlantId, LineId)`.

### ⚠️ Known gap: seed data migration

`Migrations/20260930045406_RemoveSeedData.cs` is a complete no-op (empty `Up()`/`Down()`), and
`Data/ApplicationDbContext.cs` has `SeedData.Seed(builder)` commented out in
`OnModelCreating`. Despite the name, this does **not** currently remove anything: the very
first migration (`InitialCreate`) bakes in `InsertData` calls for 1 Plant ("MAH-XUV"), 1 Line
("TCF"), 4 Shifts, 3 Departments, 8 Indicators, 9 Units, 3 ProductModels, and 24 KpiMasters —
and nothing downstream deletes them. So **a brand-new database migrated from scratch still
ends up with that starter dataset**, even though the current model/snapshot looks like it
shouldn't. `DEPLOYMENT.md` §10 actually expects this ("Check Master Data and KPI Master show
the seeded Plant/Line/Indicators/Units/KPIs"), so it's a documentation/intent mismatch rather
than a broken deploy — but if you ever want a genuinely empty fresh install, you'll need to
add a real migration with `DeleteData` calls, or decide to keep and document the seed as the
intentional starter dataset.

---

## 6. Folder structure

```
ProductionMeeting/
├── Controllers/        One controller per feature area (see §8 for every action)
├── Data/                ApplicationDbContext, RoleSeeder, SeedData
├── Helpers/              PmDates, KpiStatusCalculator, DefectParser, Roles, PolicyNames,
│                         PmClaimTypes, ClaimsPrincipalExtensions
├── Middleware/           ExceptionHandlingMiddleware
├── Migrations/           EF Core migration history (chronological, see §5)
├── Models/               EF entity classes (one file per table, roughly)
├── Services/             Interface + implementation per feature (see §7)
│   └── Authentication/   WindowsUserClaimsTransformation
├── ViewModels/            Grouped by feature folder, one view model set per page/flow
├── Views/                 Razor views, grouped by controller, + Shared/ layout & partials
├── wwwroot/               site.css, site.js, user-form.js, vendored Bootstrap/jQuery
├── Sharing Data/          Reference-only material (mockup dashboards, a sample SP draft) -
│                          not served by the app, not part of the build
└── docs/                  Older/reference copies of a few views + a raw SQL script - also
                           not part of the live app (worth confirming whether to keep)
```

---

## 7. Services — what each one does

Every service has an `I<Name>Service` interface in `Services/` and one implementation class.
Controllers only ever talk to the interface.

### `IDashboardService` / `DashboardService`
Builds the classic operational **Dashboard** (`/Dashboard/Index`): resolves the user's
accessible Plant/Line, snaps the requested week to its Monday, loads that week's KPIs +
transactions, computes Status/Variance via `KpiStatusCalculator`, and builds summary cards.
**Calls two external stored procedures** defensively (try/catch, logs and returns `null` on
failure so a missing SP never crashes the page): `dbo.usp_GetStraightPassRatio` and
`dbo.usp_GetTraceability`.

### `IExecutiveDashboardService` / `ExecutiveDashboardService`
Builds the **Executive Dashboard** (`/Dashboard/Executive`) — same Plant/Line/week resolution,
KPI cards grouped by Indicator, Highlights (first 5 Green KPIs) / Lowlights (first 5 Red),
plus `SaveRemarksAsync` (saves free text onto the `MeetingSession`, a real working feature)
and `GetKpiTrendAsync` (last-6-weeks trend for one KPI, used by a click-through modal).
⚠️ **`TrendWeekLabels`/`TrendSeries`/`ManpowerSeries`/`TopDefects` on the view model are never
populated by this service** — they stay empty lists. `Helpers/DefectParser.cs` exists
specifically to parse a "Top 5 Defects" breakdown out of Rework KPI remarks, but nothing
currently calls it. If you want those charts working again, that's the place to wire back up.

### `IKpiMasterService` / `KpiMasterService`
Backs the **KPI Master** admin screen (`/KpiMaster`) — CRUD for `KpiMaster` rows. Enforces
one business rule: no two KPIs may share `(PlantId, LineId, IndicatorId, Description)`. Only
persists `StoredProcedureName` when `IsSourcedFromStoredProcedure` is true (clears it
otherwise, so you can't have a dangling proc name on a manually-entered KPI).

### `IMasterDataService` / `MasterDataService`
Backs the tabbed **Master Data** admin screen (`/MasterData`) — CRUD for Plants, Lines,
Shifts, Departments, Models (vehicles), Units, Indicators, all through one page. Each
`Save*Async` validates required fields and (for Plant/Line) a uniqueness rule, then
upserts. No stored procedures.

### `IProductionMeetingService` / `ProductionMeetingService`
The largest and most business-rule-heavy service — backs the **Production Meeting** flow
(`/ProductionMeeting`): the session history list, opening/creating the weekly Entry grid,
saving it, and the read-only Details + audit history view. This is where
`ComputeCumulativeValues` (Month Cum./YTD math, §4), the F26/F27 pre-fill-with-override logic,
the stored-procedure auto-fill for SP-sourced KPIs, and the full field-level audit log all
live. `GetOrCreateEntryAsync` is also where access control is enforced before a session is
ever created or shown (`IUserAccessService.CanAccessLineAsync`).

### `IReportService` / `ReportService`
Backs the **Reports** page (`/Reports`) — a flattened, filterable, paginated view over every
`KpiTransaction` (joined to Session/Plant/Line/KPI/Indicator/Unit), plus an unpaginated
version of the same query for Excel export. No stored procedures.

### `IUserAccessService` / `UserAccessService`
The central access-control gate (§3) — every other service that needs to scope data to a
user calls this rather than re-implementing the rule.

### `IUserManagementService` / `UserManagementService`
Backs **User Management** (`/UserManagement`, Admin-only) — create/edit users (no password,
Windows account only), assign a single role, and grant/replace their `UserPlantLineAccess`
rows. Admins are never given access rows (they don't need them).

### Stored-procedure callers, summarized
| Caller | Procedure | Parameters | Returns |
|---|---|---|---|
| `DashboardService` | `dbo.usp_GetStraightPassRatio` | `@PlantId, @LineId, @WeekStart` | decimal |
| `DashboardService` | `dbo.usp_GetTraceability` | `@PlantId, @LineId, @WeekStart` | decimal |
| `ProductionMeetingService` | whatever's in `KpiMaster.StoredProcedureName` | `@PlantId, @LineId, @WeekStart` | decimal |

All three are called through `_db.Database.SqlQuery<decimal>(FormattableString)` — EF Core
parameterizes every interpolated value automatically, so this is safe from SQL injection even
though it reads like string interpolation. **None of these procedures exist in this
codebase** — they're expected to already exist in the target SQL Server database. If one is
missing, the call fails, gets logged as a warning, and the UI just shows "-" or leaves the
cell blank instead of crashing.

---

## 8. Controllers & routes

| Controller | Policy (class-level) | Key actions |
|---|---|---|
| `AccountController` | `[AllowAnonymous]` | `AccessDenied` — the landing page for 401/403/404 |
| `DashboardController` | `RequireViewerOrAbove` | `Index`, `Executive`, `SaveRemarks` (POST, `RequireProductionUserOrAbove`), `GetKpiTrend` (JSON) |
| `HomeController` | fallback policy | `Index` → redirects to Dashboard; `Error` (`[AllowAnonymous]`) |
| `KpiMasterController` | `RequireAdmin` | `Index`, `Create`/`Edit` (GET+POST), `SetActive`, `GetLinesForPlant` (JSON) |
| `MasterDataController` | `RequireAdmin` | `Index`, one `Save*` action per entity type, `SetActive` |
| `ProductionMeetingController` | `RequireViewerOrAbove` | `Index`, `GetLines` (JSON), `Entry` (GET, `RequireProductionUserOrAbove`), `Details`, `SaveEntry` (POST, `RequireProductionUserOrAbove`) |
| `ReportsController` | `RequireViewerOrAbove` | `Index`, `Export` (returns .xlsx via ClosedXML) |
| `UserManagementController` | `RequireAdmin` | `Index`, `Create`/`Edit` (GET+POST), `SetActive`, `GetLinesForPlant` (JSON) |

Every POST action that changes data has `[ValidateAntiForgeryToken]` and, since
`AutoValidateAntiforgeryTokenAttribute` is registered globally in `Program.cs`, so does every
other unsafe-verb action by default. The token travels via a custom header, `X-CSRF-TOKEN`
(see `wwwroot/js/site.js`'s `pmFetch` helper and jQuery's `ajaxSetup`), read from a hidden form
in `_Layout.cshtml` (`#antiForgeryForm`).

---

## 9. Views map

```
Views/
├── Dashboard/        Index.cshtml (classic Dashboard), Executive.cshtml (styled Executive Dashboard)
├── KpiMaster/         Index, Create, Edit, _KpiForm (shared partial)
├── MasterData/        Index (single tabbed page, all 7 master lists, AJAX modals)
├── ProductionMeeting/ Index (history list + quick-start), Entry (editable weekly grid),
│                      Details (read-only + audit history)
├── Reports/            Index (filterable table + Excel export button)
├── UserManagement/     Index, Create, Edit, _UserForm (shared partial)
├── Account/             AccessDenied
├── Home/                Index (dead code — controller always redirects before this renders)
└── Shared/
    ├── _Layout.cshtml        Main layout: sidebar + header + antiforgery form + confirm modal
    │                         + AJAX loader overlay; loads Bootstrap/Toastr/site.js
    ├── _AuthLayout.cshtml    Exists but unused — _ViewStart.cshtml always uses _Layout
    ├── _Header.cshtml, _Sidebar.cshtml   Layout partials
    ├── _ValidationScriptsPartial.cshtml  jQuery Validation scripts, pulled in by forms
    └── Error.cshtml          Generic error page (target of global exception handler)
```

`Views/_ViewImports.cshtml` brings every `Models`/`ViewModels`/`Services`/`Helpers` namespace
into scope globally, so views can reference types without a `using` at the top.

---

## 10. Program.cs — what's wired up and why

- **Antiforgery globally enforced** on every unsafe verb (`AutoValidateAntiforgeryTokenAttribute`
  registered as a global MVC filter), header name `X-CSRF-TOKEN`.
- **EF Core** registered against `ConnectionStrings:DefaultConnection` — this throws a startup
  exception if blank, by design, so you can never accidentally run Production against no
  database.
- **`AddIdentityCore`, not `AddIdentity`** — deliberately: only the user/role store and
  `UserManager`/`RoleManager` are needed; there's no password/cookie sign-in to wire up.
- **Negotiate authentication** (`AddAuthentication(NegotiateDefaults...).AddNegotiate()`) +
  `WindowsUserClaimsTransformation` registered as `IClaimsTransformation` to map the Windows
  identity onto app roles on every request.
- **Policies** built from `Helpers/Roles.cs` + `Helpers/PolicyNames.cs`, plus a
  `FallbackPolicy` requiring *some* authenticated user everywhere by default.
- **On startup**: `db.Database.Migrate()` runs automatically (no manual `dotnet ef database
  update` needed in any environment), then `RoleSeeder.SeedAsync` ensures the 4 roles exist.
- **`UseStatusCodePages` (not `UseStatusCodePagesWithReExecute`)**, deliberately: it only
  redirects 403/404 to a friendly page and leaves 401 completely untouched, because the
  Negotiate handshake's `WWW-Authenticate` challenge must reach the browser unmodified — the
  `WithReExecute` variant would swallow that header and break Windows Auth entirely.

---

## 11. `PmDates` — the trickiest helper in the app, explained carefully

Everything about "which week" and "which fiscal year" routes through
`Helpers/PmDates.cs`. Two sets of concerns live side by side here and it's easy to mix them
up:

1. **The `<input type="week">` HTML picker's own encoding.** Browsers that support this
   control always use the ISO-8601 calendar week number (`ISOWeek.GetWeekOfYear`), which
   resets on 1-January. `ToWeekInputValue`/`FromWeekInputValue`/`GetMondayOfWeek` use this —
   and **must keep using it** — purely so the picker's native UI and our server-side parsing
   agree on what a given `"yyyy-Www"` string means. This is never shown to a user as a label.
2. **Human-facing "Week N" labels.** These use `FiscalWeekNumber`, which counts weeks from the
   Monday-Sunday week containing **1-April**, not 1-January. This is a genuinely different
   number from #1 for most of the year.

The subtlety: **1-April isn't always a Monday.** Whichever week contains it can start a day
or two inside March. `FiscalYearEndYearOfWeek` exists to classify that *whole straddling
week* consistently by its **last day (Sunday)** rather than its first — so that one week is
"Week 1 of the new fiscal year" everywhere in the app (the week number, the F26/F27 column
header, the FY summary label, and the Month-Cum/YTD fiscal-year-reset check all agree). If you
ever add a new fiscal-year-aware calculation, call `FiscalYearEndYearOfWeek`, not the plain
`FiscalYearEndYear`, unless you're certain the date you have is never a week-straddling edge
case.

---

## 12. External dependencies not in this repo

- **SQL Server** itself, obviously — connection string in `appsettings.Development.json`
  locally, environment variable in production (see `DEPLOYMENT.md`).
- **Stored procedures** `dbo.usp_GetStraightPassRatio`, `dbo.usp_GetTraceability`, and
  whatever's configured per-KPI in `KpiMaster.StoredProcedureName` — these must be created
  directly in the database; nothing in this codebase defines them. `Sharing Data/SP for
  PressShop.txt` may be a draft of one; worth checking if you're about to add a new one.
- **CDN-hosted JS/CSS**: Bootstrap/Bootstrap Icons/Toastr/DataTables/Chart.js are loaded from
  CDN links in `_Layout.cshtml` (and `Executive.cshtml` for Chart.js specifically) — vendored
  copies of Bootstrap/jQuery also exist locally under `wwwroot/lib/` as a fallback set.

---

## 13. Things that look like they should do something but don't (yet)

- **Executive Dashboard trend/manpower/defects charts** — the view model has the fields
  (`TrendWeekLabels`, `TrendSeries`, `ManpowerSeries`, `TopDefects`), the view has the chart
  panels, `Helpers/DefectParser.cs` exists specifically to parse defect breakdowns out of
  Rework KPI remarks — but `ExecutiveDashboardService.GetDashboardAsync` never populates any
  of them. They currently render empty. This was a deliberate placeholder decision made
  earlier in the project (to show sample data while other parts stay real), then the KPI-card
  sections were switched back to live data in a later edit — the chart-feeding code for this
  specific set of fields just hasn't been reconnected since.
- **Vehicle Model dropdown removed from the Entry grid** — by request, the weekly entry screen
  no longer lets you pick a `ProductModel` per row. The `ProductModel` entity, the Master Data
  admin screen for it, and `KpiTransaction.ModelId` **all still exist** in the schema (nothing
  was dropped at the DB level) — new transactions just always save `ModelId = null`. If you
  ever need per-model KPI tracking again, the plumbing is still there, just unused.
- **`Views/Shared/_AuthLayout.cshtml`** exists but no view references it — `_ViewStart.cshtml`
  always uses `_Layout`. Safe to ignore or delete if you're cleaning up.
- **`docs/` folder** at the repo root holds older/reference copies of a few KpiMaster views, an
  older `ProductionMeetingController.cs`, and a raw SQL script (`Production Meeting Excel
  3.sql`) — these are **not** part of the live app (the real source is under `Views/` and
  `Controllers/`). Worth deciding whether to keep as historical reference, move out of the
  repo, or delete.
- **Seed-data migration gap** — see the ⚠️ box at the end of §5.

---

## 14. Common changes — where to go

**Add a new KPI** → KPI Master screen (`/KpiMaster/Create`), Admin only. No code change
needed — it's fully data-driven (Plant/Line/Indicator/Unit all pick from existing master
data).

**Add a new Plant, Shop, Unit, Indicator, Shift, or vehicle Model** → Master Data screen
(`/MasterData`), Admin only. Same — no code change.

**Add a new role, or change what a role can do** → `Helpers/Roles.cs` (add the constant),
`Helpers/PolicyNames.cs` + the `AddPolicy` calls in `Program.cs` (define what it grants), then
add `[Authorize(Policy = ...)]` wherever needed. Remember policies are hierarchical by
`RequireRole(...)` listing every role that should pass, not actually hierarchical by
framework magic — so a new role needs to be explicitly added to every policy it should satisfy.

**Change the Green/Amber/Red tolerance band** → `Helpers/KpiStatusCalculator.Compute` (the 5%
figure). One place, used by both dashboards.

**Wire up a new stored-procedure-backed figure** → follow the existing pattern in
`DashboardService` (`TryGetScalarAsync`) or `ProductionMeetingService`
(`TryGetScalarFromProcedureAsync`): wrap the `_db.Database.SqlQuery<decimal>($"EXEC
proc @Param = {value}, ...")` call in try/catch, log a warning on failure, return `null` rather
than letting it throw.

**Reconnect the Executive Dashboard's Trend/Manpower/Top-Defects charts** → populate
`filters.TrendWeekLabels`/`TrendSeries`/`ManpowerSeries`/`TopDefects` inside
`ExecutiveDashboardService.GetDashboardAsync`. `Helpers/DefectParser.Parse(remarks)` is
already written and ready to use for the defects piece — it just needs to be called against
each Rework KPI's weekly `Remarks` and aggregated.

**Change any schema** (add/remove/rename a column, table, or relationship) → edit the `Models/`
class and, if needed, the Fluent API config in `Data/ApplicationDbContext.cs`, then from the
project folder:
```powershell
dotnet ef migrations add <DescriptiveName>
dotnet ef database update
```
Both commands need `dotnet tool restore` run once first if `dotnet-ef` isn't already
installed locally (it's pinned in `.config/dotnet-tools.json`).

**Change the fiscal year definition itself** (e.g. if it ever needs to start in a different
month) → `PmDates.FiscalYearEndYear` is the one place that decides the April cutoff; everything
else (`FiscalWeekNumber`, `FiscalYearEndYearOfWeek`, `FiscalTargetLabels`, the Month/YTD reset
checks) is built on top of it.

**Run the app locally**:
```powershell
dotnet build
dotnet run --launch-profile https
```
Needs a reachable SQL Server matching `appsettings.Development.json`'s connection string
(`Server=localhost;Database=ProductionMeetingDb;...` by default) — migrations apply themselves
on startup.
