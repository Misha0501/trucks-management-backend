# VervoerManager Backend – Context for AI Assistants

**Audience**: AI coding assistants (Cursor, Claude, etc.) and developers helping on this project.

## Start Here

1. **Read `docs/INDEX.md` first** – Master map of all documentation.
2. **Read `docs/BACKEND_GUIDE.md`** – Full technical overview.
3. For a specific feature, read `docs/features/<FEATURE>.md`.
4. For API/auth/data, read `docs/api/`, `docs/auth/`, `docs/data/`.
5. For services/middleware/database, read `docs/core/`.

## Project Identity

- **Name**: VervoerManager (Truck Management System)
- **Repo**: https://github.com/Misha0501/trucks-management-backend
- **Live API**: https://api.vervoermanager.nl
- **Stack**: .NET 9, ASP.NET Core, PostgreSQL, EF Core, Docker, QuestPDF, Telegram.Bot

## Quick Reference

- **Entry**: `Program.cs` – DI, middleware, endpoint mapping
- **Endpoints**: `TruckManagement/Endpoints/*.cs` – One file per domain (35+ files)
- **Entities**: `TruckManagement/Entities/` – 45+ entities
- **DbContext**: `Data/ApplicationDbContext.cs` – DbSets, OnModelCreating, global query filters
- **Services**: `Services/` – DriverContractService, DriverInvoiceService, TelegramNotificationService, etc.
- **Interfaces**: `Interfaces/` – IEmailService, IContractStorageService, ITelegramNotificationService
- **Response**: `ApiResponseFactory.Success(data)` / `ApiResponseFactory.Error(message)`
- **Auth**: `[Authorize]` or `[Authorize(Roles = "...")]`. JWT from `Authorization: Bearer <token>`.

## Critical Paths

| Path | Purpose |
|------|---------|
| `Program.cs` | DI, middleware order, endpoint registration |
| `Endpoints/AuthEndpoints.cs` | /login, /register, /forgotpassword |
| `Endpoints/DriverEndpoints.cs` | Drivers, contracts, periods (large file) |
| `Endpoints/ClientEndpoints.cs` | Clients (Routes namespace: RegisterClientsRoutes) |
| `Data/ApplicationDbContext.cs` | DbSets, relationships, query filters |
| `Middlewares/GlobalExceptionHandlingMiddleware.cs` | Catches all exceptions, PostgreSQL error mapping |
| `Helpers/ApiResponseFactory.cs` | Success/Error response builders |
| `Utilities/JwtTokenHelper.cs` | JWT generation |

## Key Conventions

- **No `/api/` prefix** – Routes at root (e.g. `/login`, `/drivers`)
- **Minimal APIs** – `app.MapGet/Post/Put/Delete(path, handler)`
- **Pagination**: `pageNumber`, `pageSize` query params
- **Global query filters** – Company, Client, Driver, etc. filtered by IsDeleted, IsApproved. Use `IgnoreQueryFilters()` when needed (e.g. seeding, admin views).

## When Implementing a Feature

1. Read the relevant `docs/features/<FEATURE>.md`
2. Update it (or create it) when you change behavior
3. Add endpoints to `docs/api/ENDPOINTS.md`
4. Add service to `docs/core/SERVICES.md` if new
5. Run migrations after schema changes: `dotnet ef database update`
6. See `docs/CONTRIBUTING_DOCS.md` for the full checklist

## Related Repos

- **Frontend**: https://github.com/andreyroizin/trucks-management-frontend
- **Requirements**: Frontend repo `plans/requirments/`, `docs/requirements/`
