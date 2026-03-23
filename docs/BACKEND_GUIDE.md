# VervoerManager Backend – Technical Guide

## Tech Stack

| Component | Technology |
|-----------|------------|
| **Framework** | .NET 9, ASP.NET Core |
| **Database** | PostgreSQL 15, Npgsql, Entity Framework Core 9 |
| **Auth** | ASP.NET Identity, JWT Bearer |
| **API** | Minimal APIs (endpoint extensions) |
| **PDF** | QuestPDF (DriverContractPdfBuilder, DriverInvoicePdfBuilder) |
| **Email** | SMTP (SmtpEmailService) |
| **Telegram** | Telegram.Bot for driver notifications |
| **Localization** | .resx files (en, nl, bg), `Accept-Language` header |

---

## Folder Structure

```
TruckManagement/
  Program.cs                    # Entry: DI, middleware, endpoint mapping
  appsettings.json             # Config (ConnectionStrings, JwtSettings, Smtp, Storage, Telegram)
  appsettings.Development.json  # Dev overrides

  Data/
    ApplicationDbContext.cs    # EF DbContext, DbSets, OnModelCreating, query filters
    DatabaseSeeder.cs           # Startup: MigrateAsync, CAODataSeeder, roles, optional test data

  Entities/                     # EF entities (45+ files)
    Company.cs, Client.cs, Driver.cs, Car.cs, Ride.cs, PartRide.cs, ...

  DTOs/                         # Request/response DTOs
    *.Request.cs, *.Dto.cs, Reports/*.cs

  Endpoints/                    # API endpoints (one file per domain)
    AuthEndpoints.cs            # /login, /register, /forgotpassword, /reset-password-token
    UserEndpoints.cs            # /users/me, /users, /users/{id}, /contactpersons, /drivers/{id}
    CompanyEndpoints.cs         # /companies
    ClientEndpoints.cs          # /clients (namespace TruckManagement.Routes, RegisterClientsRoutes)
    DriverEndpoints.cs          # /drivers, contracts, periods
    CarEndpoints.cs             # /cars, /car-files
    RideEndpoints.cs            # /rides
    PartRideEndpoints.cs        # /partrides, /partride-files
    DisputeRoute.cs             # /disputes
    EmployeeContractsEndpoints.cs
    ... (35+ endpoint files)

  Extensions/
    DatabaseServiceCollectionExtensions.cs  # AddPostgresDatabase
    IdentityServiceCollectionExtensions.cs  # AddAppIdentity
    JwtServiceCollectionExtensions.cs       # AddJwtAuthentication
    AuthorizationPolicies.cs               # AddAuthorizationPolicies (EmployeeOnly, etc.)
    PartRideExtensions.cs                   # PartRide helpers

  Middlewares/
    GlobalExceptionHandlingMiddleware.cs   # Catches all exceptions, returns ApiResponse

  Services/
    DriverCompensationService.cs
    DriverContractPdfBuilder.cs
    DriverContractService.cs
    DriverInvoicePdfBuilder.cs
    DriverInvoiceService.cs
    LocalContractStorageService.cs          # IContractStorageService
    PartRideCalculator.cs
    RideExecutionCalculationService.cs
    TelegramNotificationService.cs         # ITelegramNotificationService
    SmtpEmailService.cs                     # IEmailService (in Interfaces)
    Reports/
      ReportCalculationService.cs
      VacationCalculator.cs, TvTCalculator.cs, OvertimeClassifier.cs

  Interfaces/
    IEmailService.cs
    IContractStorageService.cs
    ITelegramNotificationService.cs

  Options/
    TelegramOptions.cs          # BotToken, BotUsername
    StorageOptions.cs           # BasePath, BasePathCompanies, TmpPath, SignedContractsPath

  Helpers/
    ApiResponseFactory.cs       # Success(data), Error(message)

  Models/
    ApiResponse.cs              # record ApiResponse<T>(IsSuccess, StatusCode, Data, Errors)

  Utilities/
    JwtTokenHelper.cs           # GenerateJwtToken

  Resources/                    # Localization
    SharedResource.en.resx
    SharedResource.nl.resx
    SharedResource.bg.resx

  Migrations/
    2024*.cs, 2025*.cs          # EF migrations

compose.yaml                     # Docker: truckmanagement, postgresdb, pgadmin
```

---

## Middleware Pipeline (Order)

1. `UseRequestLocalization` – Culture from `Accept-Language`
2. `UseCors("AllowAll")`
3. `UseMiddleware<GlobalExceptionHandlingMiddleware>` – Global exception handler
4. (At startup) `DatabaseSeeder.SeedAsync` – Apply migrations, seed roles, optional test data
5. (Dev only) `MapOpenApi` – Swagger/OpenAPI
6. `UseHttpsRedirection`
7. `UseAuthentication` – JWT Bearer
8. `UseAuthorization`
9. Endpoints (MapAuthEndpoints, MapUserEndpoints, ...)

---

## Endpoint Registration (Program.cs)

All endpoints are registered via extension methods. **No base `/api/` prefix** – routes are at root (e.g. `/login`, `/drivers`).

| Extension | File | Domain |
|-----------|------|--------|
| MapAuthEndpoints | AuthEndpoints.cs | Login, register, password reset |
| MapUserEndpoints | UserEndpoints.cs | Users, contact persons |
| MapCompanyEndpoints | CompanyEndpoints.cs | Companies |
| MapRoleEndpoints | RoleEndpoints.cs | Roles |
| RegisterClientsRoutes | ClientEndpoints.cs (Routes) | Clients |
| MapDriversEndpoints | DriverEndpoints.cs | Drivers, contracts, periods |
| MapContactPersonsEndpoints | ContactPersonsEndpoints.cs | Contact persons |
| MapSurchargeEndpoints | SurchargeEndpoints.cs | Surcharges |
| MapRateEndpoints | RateEndpoints.cs | Rates |
| MapUnitEndpoints | UnitEndpoints.cs | Units |
| MapCarEndpoints | CarEndpoints.cs | Cars |
| MapCharterEndpoints | CharterEndpoints.cs | Charters |
| MapRideEndpoints | RideEndpoints.cs | Rides |
| MapPartRideEndpoints | PartRideEndpoints.cs | Part rides |
| MapHoursCodeRoutes | HoursCodeEndpoints.cs | Hours codes |
| MapHoursOptionRoutes | HoursOptionEndpoints.cs | Hours options |
| MapEmployeeContractsEndpoints | EmployeeContractsEndpoints.cs | Contracts |
| MapFileUploadsEndpoints | FileUploadsEndpoints.cs | Temporary uploads |
| MapPartRideFilesEndpoints | PartRideFilesEndpoint.cs | Part ride files |
| MapCarFilesEndpoints | CarFilesEndpoint.cs | Car files |
| MapDriverFilesEndpoints | DriverFilesEndpoint.cs | Driver files |
| MapDisputeEndpoints | DisputeRoute.cs | Disputes |
| MapWeekToSubmitEndpoints | WeekToSubmitEndpoints.cs | Weeks to submit |
| MapReportEndpoints | ReportEndpoints.cs | Reports |
| MapCapacityTemplateEndpoints | CapacityTemplateEndpoints.cs | Capacity templates |
| MapWeeklyPlanningEndpoints | WeeklyPlanningEndpoints.cs | Weekly planning |
| MapRideAssignmentEndpoints | RideAssignmentEndpoints.cs | Ride assignment |
| MapDailyPlanningEndpoints | DailyPlanningEndpoints.cs | Daily planning |
| MapAvailabilityEndpoints | AvailabilityEndpoints.cs | Driver/truck availability |
| MapRideExecutionEndpoints | RideExecutionEndpoints.cs | Ride executions |
| MapRideExecutionFileEndpoints | RideExecutionFileEndpoints.cs | Execution files |
| MapRideExecutionCommentEndpoints | RideExecutionCommentEndpoints.cs | Execution comments |
| MapRideExecutionDisputeEndpoints | RideExecutionDisputeEndpoints.cs | Execution disputes |
| MapRideWeekSubmissionEndpoints | RideWeekSubmissionEndpoints.cs | Week submission |
| MapRidePeriodEndpoints | RidePeriodEndpoints.cs | Period signing |
| MapTelegramEndpoints | TelegramEndpoints.cs | Telegram webhook, registration |
| MapTelegramTestEndpoints | TelegramTestEndpoints.cs | Telegram poll (testing) |
| MapDriverInvoiceEndpoints | DriverInvoiceEndpoints.cs | Driver invoices |

---

## API Conventions

- **Response**: Use `ApiResponseFactory.Success(data)` or `ApiResponseFactory.Error(message)` – returns `ApiResponse<T>` JSON
- **Auth**: `[Authorize]` or `[Authorize(Roles = "globalAdmin,customerAdmin")]` on protected endpoints
- **Pagination**: Query params `pageNumber` (1-based), `pageSize`. Response: `totalCount`, `totalPages`, `pageNumber`, `pageSize`, `data`
- **IDs**: All entity IDs are `Guid`. Path params: `{id:guid}` or `{id}`

---

## Database

- **Provider**: Npgsql (PostgreSQL)
- **Connection**: `ConnectionStrings__DefaultConnection` from appsettings or env
- **Global query filters** (ApplicationDbContext.OnModelCreating):
  - `Company`: `!IsDeleted && IsApproved`
  - `Client`: `!IsDeleted && IsApproved`
  - `ApplicationUser`: `!IsDeleted && IsApproved`
  - `Driver`: `!IsDeleted`
  - `ContactPerson`: `!IsDeleted`
  - `Car`, `Charter`, `Rate`, `Surcharge`: Via navigation to Company/Client

- **Migrations**: `dotnet ef migrations add <Name>` (from TruckManagement folder), then `dotnet ef database update`
- **Seeding**: `DatabaseSeeder.SeedAsync` runs at startup – CAO data, roles, optional admin user. Test data is commented out.

---

## Configuration (appsettings)

| Section | Keys | Purpose |
|---------|------|---------|
| ConnectionStrings | DefaultConnection | PostgreSQL connection |
| JwtSettings | SecretKey, Issuer, Audience | JWT signing |
| FrontEnd | ResetPasswordUrl | Password reset link base |
| Storage | BasePath, BasePathCompanies, TmpPath, SignedContractsPath | File storage paths |
| Smtp | Host, Port, Username, Password, FromAddress | Email |
| Telegram | BotToken, BotUsername | Telegram bot |
| Logging | LogLevel | Logging |

---

## Key Patterns

1. **Endpoint pattern**: `app.MapGet/Post/Put/Delete(path, [Authorize] async (deps) => { ... })`
2. **DI**: Services injected into endpoint handlers. Scoped: DbContext, most services. Singleton: TelegramNotificationService.
3. **Localization**: `IResourceLocalizer` for localized strings. Culture from `Accept-Language`.
4. **File storage**: `IContractStorageService` (LocalContractStorageService). Storage paths from `StorageOptions`.
5. **Telegram**: Webhook at `/telegram/webhook`. Fire-and-forget notifications via `ITelegramNotificationService`.
