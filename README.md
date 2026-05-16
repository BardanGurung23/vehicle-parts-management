# Backend Overview

The backend is an ASP.NET Core Web API solution organized around Clean Architecture and a single EF Core migration authority for the final PostgreSQL schema.

## 📌 Documentation Sync Rule

| 📄 Document | Purpose | Update rule |
| --- | --- | --- |
| `backend/README.md` | Backend setup, API scope, schema workflow, verification snapshot | Update when backend behavior, setup, or verification changes |
| `frontend/README.md` | Active frontend runtime and feature status | Update when frontend flows or runtime paths change |

When backend endpoints, DTOs, commands, architecture boundaries, or verification results change, update this file, `frontend/README.md`, and `doc/progress.md` in the same change set whenever shared project status also changes.

## 🧭 Quick Snapshot

| 🏷️ Area | 🚦 Status | Notes |
| --- | --- | --- |
| Architecture | ✅ Complete | Flow remains Controller -> Service -> Repository -> DbContext |
| Auth | ✅ Implemented | Canonical self-registration and login live under `/api/auth/*` |
| Final schema | ✅ Implemented | EF Core baseline migration is the single schema authority |
| Dev/test reset | ✅ Implemented | Guided preflight validates PostgreSQL availability, protects non-demo local data, and recreates incompatible local DBs only after explicit confirmation |
| Customer flows | ✅ Implemented | Staff create/search/detail plus customer signup, profile update, and vehicle add/edit/remove flows are live |
| Staff admin | ✅ Implemented | Staff list, create, roles, and role update are live |
| Vendors | ✅ Implemented | Vendor CRUD is available for admin users |
| Appointments | ✅ Implemented | Customer booking and admin status management are live |
| Sales | ✅ Implemented | Sales invoices, totals, and loyalty discount are live |
| Purchase invoices | ✅ Implemented | Admin purchase-invoice creation and recent-invoice listing are live |
| Notifications/reports | ✅ Implemented | Financial reports, customer reports, and automated alerts are in place |

## 🏗️ Solution Structure

| 🧩 Project | Responsibility |
| --- | --- |
| `Vpims.API` | Controllers, auth, middleware, API entry points |
| `Vpims.Application` | DTOs, interfaces, common contracts |
| `Vpims.Domain` | Core entities and business models |
| `Vpims.Infrastructure` | Repositories, services, persistence, DI wiring |
| `tests/Vpims.Member4.Backend.Tests` | Focused backend test coverage |
| `tools/` | Bootstrap, seeding, and demo dataset utilities |

## 🗃️ Final Database Workflow

| 🧱 Concern | Final behavior |
| --- | --- |
| Schema authority | EF Core migrations under `Vpims.Infrastructure/Migrations` |
| Guided startup path | `Vpims.DatabasePreflight` validates host readiness, schema compatibility, and reset safety before startup |
| Direct API startup | `DatabaseInitializer` now fails fast if the database requires a guided destructive reset |
| Mismatch handling | Incompatible local DBs require explicit confirmation, and resets are blocked by default when non-demo or unclassified local data exists |
| Seed source | Canonical demo dataset plus the staff-sales seed are inserted during initialization |
| Legacy SQL | `backend/sql/basic-sql.sql` is not the local development source of truth |
| Expected outcome for cloned projects | Local development converges to the committed final schema automatically |

## 🔌 Canonical API Summary

| 🌐 Area | Endpoints |
| --- | --- |
| Auth | `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/me` |
| Customer self-service | `POST /api/customers/register`, `GET /api/customers/me`, `PUT /api/customers/me`, `GET /api/customers/me/vehicles`, `POST /api/customers/me/vehicles`, `PUT /api/customers/me/vehicles/{vehicleId}`, `DELETE /api/customers/me/vehicles/{vehicleId}` |
| Staff customer management | `POST /api/customers`, `GET /api/customers/search`, `GET /api/customers/{customerId}` |
| Staff admin | `GET /api/admin/staff`, `POST /api/admin/staff`, `GET /api/admin/staff/roles`, `PUT /api/admin/staff/{userId}/role` |
| Dashboard | `GET /api/dashboard/summary` |
| Parts | List, detail, create, update, and delete endpoints under `/api/parts` |
| Vendors | CRUD endpoints under `/api/admin/vendors` |
| Appointments | Customer and admin flows under `/api/appointments` |
| Sales | Customer and employee-assisted flows under `/api/sales` |
| Part requests | Customer and admin flows under `/api/part-requests` |
| Reviews | Customer review flows under `/api/reviews` |

## 👥 Backend Feature Coverage

| 🎯 Feature Area | 🚦 Status | Backend support |
| --- | --- | --- |
| Feature 5: Vendor management | ✅ Implemented | Admin users can manage vendors |
| Feature 6: Customer registration | ✅ Implemented | Staff can create customers with an initial vehicle |
| Feature 7: Sales invoices | ✅ Implemented | Sales creation, invoice numbering, item totals, and retrieval are live |
| Feature 10: Customer search | ✅ Implemented | Search by ID, phone, vehicle number, or name |
| Feature 12: Customer self-service | ✅ Implemented | Self-registration with optional initial vehicle capture, current customer detail, profile update, and vehicle add/edit/remove are live |
| Feature 13: Customer requests/reviews/appointments | ✅ Implemented | Booking, part requests, and reviews are supported |
| Feature 14: Purchase and service history | ✅ Implemented | Purchase history and appointment history endpoints are live |
| Feature 16: Loyalty discount | ✅ Implemented | 10% discount is applied to qualifying purchases |
| Feature 4: Purchase invoices | ✅ Implemented | Admin users can create and review purchase invoices |
| Feature 1 / 9 / 15 | ✅ Implemented | Financial reports, customer reports, and automated notification workflows are live |
| Feature 11 | 🟡 Partial | Invoice and alert email code paths are implemented, but live delivery still needs real SMTP credentials |

## 📈 Project Progress Snapshot

This section mirrors the current state recorded in `doc/progress.md`, formatted for backend contributors.

### 📝 Planning And Documentation

| Item | Status |
| --- | --- |
| PRD aligned with Clean Architecture exists in `doc/prd.md` | ✅ |
| Supporting architecture, ERD, style guide, question tracking, and reporting docs exist in `doc/` | ✅ |
| Clean Architecture boundaries are reflected in code | ✅ |
| Progress roadmap refreshed against the current codebase, the latest browser validation on 2026-04-29, the focused Member 4 verification refresh on 2026-04-30, the Member 1 finance/vendor implementation refresh on 2026-05-04, and the Member 3 backend verification/startup refresh on 2026-05-14 | ✅ |

### ⚙️ Backend Status Mirror

| Item | Status |
| --- | --- |
| ASP.NET Core API with controllers, JWT auth, RBAC, CORS, and exception middleware | ✅ |
| Clean Architecture solution with API, Application, Domain, Infrastructure, and CLI tooling | ✅ |
| EF Core baseline migration as the single final schema authority | ✅ |
| Cross-platform database preflight plus guided reset workflow | ✅ |
| Customer self-registration, login, current-customer detail, profile update, vehicle add/edit/remove | ✅ |
| Staff management end to end | ✅ |
| Staff/admin customer registration, search, and detail lookup | ✅ |
| Parts inventory API end to end | ✅ |
| Vendor CRUD API | ✅ |
| Appointment create/list/status management with UTC normalization | ✅ |
| Sales invoice creation and retrieval with subtotal/discount persistence | ✅ |
| Customer part-request and service-review flows | ✅ |
| Dashboard summary aggregation | ✅ |
| Local browser testing allowed by current CORS configuration | ✅ |
| Member 4 backend milestone | ✅ |
| EF persistence for users, roles, customers, vehicles, parts, vendors, appointments, part requests, service reviews, sales invoices, purchase invoices, and predictive alerts | ✅ |
| Purchase-invoice workflow delivery | ✅ Active API and frontend workflow are implemented |
| Predictive-alert and reminder delivery | 🟡 Hosted workflow and best-effort email delivery are implemented, but live SMTP verification is still pending |
| Admin/bootstrap/demo tooling | ✅ |
| Focused backend test project under `tests/` | ✅ |
| `Vpims.CleanupDemoDataset` aligned with final EF schema | ✅ |

### 🖥️ Frontend Runtime Mirror

| Item | Status |
| --- | --- |
| Router-based active app under `frontend/admin/src/app` | ✅ |
| Public, protected, admin-only, employee-only, and customer-only route gates | ✅ |
| Login and session restore | ✅ |
| Customer self-registration through canonical auth endpoint | ✅ |
| Staff management page | ✅ |
| Role-aware dashboard | ✅ |
| Customer search and dedicated detail route | ✅ |
| Active admin pages for staff, vendors, appointments, and part requests | ✅ |
| Customer profile and vehicle management routes | ✅ |
| Customer appointment routes | ✅ |
| Customer part-request and review routes | ✅ |
| Shop and purchase-history routes with employee-assisted checkout | ✅ |
| Active parts page uses the direct feature API client | ✅ |
| Staff read-only parts access with admin-only mutations | ✅ |
| Legacy inactive frontend code still exists, but inactive feature-level Redux pages have started to be removed | ⏳ |
| Frontend automated tests | ✅ |

### 🧪 Verification Mirror

| Check | Result |
| --- | --- |
| `dotnet build backend/vpims-backend.sln` | ✅ Passes |
| `bash tests/run-backend-tests.sh` | ✅ Passes |
| `dotnet test tests/Vpims.Member4.Backend.Tests/Vpims.Member4.Backend.Tests.csproj` | ✅ Passes with 36/36 tests |
| `dotnet run --project backend/tools/Vpims.DatabasePreflight` | ✅ Reports a compatible PostgreSQL database at `autonix_db` on this machine |
| `dotnet run --project backend/Vpims.API --configuration Debug` | ✅ Direct startup remains available when the database is already compatible |
| `npm --prefix frontend/admin run build` | ✅ Passes |
| `npm --prefix frontend/admin run lint` | ✅ Passes |
| `npm --prefix frontend/admin run test:run` | ✅ Passes with 3/3 tests |
| Latest documented browser smoke checks for admin login, dashboard alerts, customer reports, financial reports, purchase invoices, and customer detail history | ✅ Passes |

## ✉️ SMTP Configuration

Both invoice emails and automated alert emails use the `InvoiceEmail` section in `backend/Vpims.API/appsettings.json`, environment variables, or .NET user secrets.

Required settings:

- `InvoiceEmail__Host`
- `InvoiceEmail__Port`
- `InvoiceEmail__Username`
- `InvoiceEmail__Password`
- `InvoiceEmail__FromEmail`
- `InvoiceEmail__FromName`
- `InvoiceEmail__EnableSsl`

Example .NET user-secret setup:

```bash
dotnet user-secrets --project backend/Vpims.API set "InvoiceEmail:Host" "smtp.mailhost.local"
dotnet user-secrets --project backend/Vpims.API set "InvoiceEmail:Port" "587"
dotnet user-secrets --project backend/Vpims.API set "InvoiceEmail:Username" "mailer-user"
dotnet user-secrets --project backend/Vpims.API set "InvoiceEmail:Password" "<secret>"
dotnet user-secrets --project backend/Vpims.API set "InvoiceEmail:FromEmail" "noreply@autonix.local"
dotnet user-secrets --project backend/Vpims.API set "InvoiceEmail:FromName" "Autonix"
dotnet user-secrets --project backend/Vpims.API set "InvoiceEmail:EnableSsl" "true"
```

Default repository values are placeholders only. Local development keeps alert generation running even when SMTP is intentionally unset, but live invoice or alert delivery will not work until real mail-server values are supplied.

With placeholder values, `SmtpEmailService` rejects delivery with a validation error stating that invoice email settings are incomplete. If the SMTP server accepts a connection but rejects delivery, the service returns a validation error asking the operator to verify the `InvoiceEmail` configuration.

## 👨‍👩‍👧‍👦 Member Assignment Tracking

| 👤 Member | Assigned area | Feature status |
| --- | --- | --- |
| Anjal | Finance & Vendors (`Feature 1`, `Feature 4`, `Feature 5`) | ✅ Feature 5, ⏳ Feature 1, 🟡 Feature 4 |
| Sneha Ssapkota | Staff & Inventory (`Feature 2`, `Feature 3`, `Feature 15`) | ✅ Feature 2, 🟡 Feature 3, ⏳ Feature 15 |
| Krijal Maharjan | Sales & Invoicing (`Feature 7`, `Feature 11`, `Feature 16`) | ✅ Feature 7, ✅ Feature 16, ⏳ Feature 11 |
| Paurakh Pyakurel | Registration & Search (`Feature 6`, `Feature 12`, `Feature 10`) | ✅ All assigned features implemented |
| Bardan Gurung | Customer Portal & History (`Feature 8`, `Feature 9`, `Feature 13`, `Feature 14`) | ✅ Feature 8, ⏳ Feature 9, ✅ Feature 13, ✅ Feature 14 |

## 🚚 Latest Delivered Slice

| Delivered item | Status |
| --- | --- |
| Extended public customer signup so the active form can post an optional initial vehicle through `/api/customers/register` | ✅ |
| Added customer vehicle edit support through `PUT /api/customers/me/vehicles/{vehicleId}` | ✅ |
| Expanded focused Member 4 backend coverage for search filters, profile updates, and vehicle add/edit/remove/list flows | ✅ |
| Replaced mixed manual SQL and `EnsureCreated` with final EF baseline migration plus reset logic | ✅ |
| Added canonical demo-data seeding and aligned cleanup/bootstrap tooling | ✅ |
| Expanded schema for vendors, sales invoices, purchase invoices, service reviews, and predictive alerts | ✅ |
| Opened active customer-only routes for appointments, profile, vehicles, reviews, part requests, and purchase history | ✅ |
| Fixed role-aware navigation and routing for customer-only pages and admin part requests | ✅ |
| Added staff/admin-assisted sales on behalf of a selected customer | ✅ |
| Fixed customer vehicle add flow so the list refreshes immediately | ✅ |
| Fixed appointment booking with stable datetime input plus UTC persistence conversion | ✅ |
| Removed duplicate error rendering on customer appointments and reviews pages | ✅ |
| Fixed purchase-history subtotal/discount/total presentation | ✅ |
| Fixed Vendors and Appointments shell subtitle copy and widened appointments action area | ✅ |
| Revalidated with backend build, backend tests, frontend build, and live browser retests | ✅ |

## 🔧 Local Commands

### macOS / Linux

| Task | Command |
| --- | --- |
| Report database preflight | `dotnet run --project backend/tools/Vpims.DatabasePreflight` |
| Guided backend startup | `npm run start:backend` |
| Run backend API directly | `dotnet run --project backend/Vpims.API --configuration Debug` |
| Run frontend admin app | `npm --prefix frontend/admin run start` |
| Build backend solution | `dotnet build backend/vpims-backend.sln` |
| Build frontend admin app | `npm --prefix frontend/admin run build` |
| Run backend tests | `bash tests/run-backend-tests.sh` |
| Bootstrap first admin | `bash backend/scripts/bootstrap-first-admin.sh "Full Name" email@example.com 9800000000 password` |

### Windows PowerShell

| Task | Command |
| --- | --- |
| Report database preflight | `dotnet run --project backend/tools/Vpims.DatabasePreflight` |
| Guided backend startup | `npm run start:backend` |
| Run backend API directly | `dotnet run --project backend/Vpims.API --configuration Debug` |
| Run frontend admin app | `npm --prefix frontend/admin run start` |
| Build backend solution | `dotnet build backend/vpims-backend.sln` |
| Build frontend admin app | `npm --prefix frontend/admin run build` |
| Run backend tests | `bash tests/run-backend-tests.sh` |
| Bootstrap first admin | `bash backend/scripts/bootstrap-first-admin.sh "Full Name" email@example.com 9800000000 password` |

Use Git Bash or WSL for the `bash` commands on Windows.

Direct API startup is now intentionally stricter than the guided startup path. If the local database needs a destructive reset, the API will abort and tell you to run the preflight tool first instead of dropping data during web-host startup.

### Default Local URLs

| Service | URL |
| --- | --- |
| Backend API | `http://localhost:5154` |
| Frontend admin app | `http://localhost:5173` |

## 👤 Demo Accounts

| Role | Accounts | Password |
| --- | --- | --- |
| Admin | `demo.admin1@autonix.local`, `demo.admin2@autonix.local` | `DemoPass123!` |
| Staff | `demo.staff1@autonix.local`, `demo.staff2@autonix.local` | `DemoPass123!` |
| Customer | `demo.customer1@autonix.local`, `demo.customer2@autonix.local`, `demo.customer3@autonix.local` | `DemoPass123!` |

## 🚀 Backend Setup Notes

| Step | Action |
| --- | --- |
| 1️⃣ | Configure `backend/Vpims.API/appsettings.Development.json` with the local PostgreSQL connection and JWT settings |
| 2️⃣ | Start the backend API with the command for your OS from the Local Commands section |
| 3️⃣ | Start the frontend admin app with the command for your OS from the Local Commands section after the API is up |
| 4️⃣ | In Development or test, startup compares the existing local database with the committed EF migration baseline |
| 5️⃣ | If the schema is missing or mismatched, the API drops and recreates the target database, applies the baseline migration, and seeds the canonical demo dataset |
| 6️⃣ | Sign in with one of the demo accounts above to verify the seeded environment |

## 🎯 Highest-Priority Follow-Up

| Priority item | Status |
| --- | --- |
| Commit a repeatable database migration workflow | ✅ |
| Add broader regression coverage for appointment UTC persistence and customer self-service vehicle flows | ✅ |
| Decide whether remaining active flows on older Redux services should move to the newer feature API layer | 🟡 Active feature routes have been migrated; some legacy admin pages mounted through the router still use Redux services |
| Complete live SMTP verification for invoice and alert emails | ⏳ |
| Expand employee-facing customer history views and related reports | ✅ |
| Clean up or retire inactive legacy frontend code | 🟡 Started; clearly inactive files removed and remaining legacy admin pages still need phased cleanup |

## ⚠️ Important Note

Do not apply `backend/sql/basic-sql.sql` manually for local development. Keep schema changes in the EF Core model and generate migrations from `Vpims.Infrastructure`.
