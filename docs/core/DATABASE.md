# Backend Database Reference

## ApplicationDbContext

**File**: `TruckManagement/Data/ApplicationDbContext.cs`

Inherits `IdentityDbContext<ApplicationUser, ApplicationRole, string>`. Contains all `DbSet<T>` for entities.

---

## DbSets

| DbSet | Entity |
|-------|--------|
| Companies | Company |
| Clients | Client |
| Drivers | Driver |
| ContactPersons | ContactPerson |
| ContactPersonClientCompanies | ContactPersonClientCompany |
| Cars | Car |
| CarFiles | CarFile |
| CarUsedByCompanies | CarUsedByCompany |
| DriverFiles | DriverFile |
| DriverUsedByCompanies | DriverUsedByCompany |
| Rides | Ride |
| Charters | Charter |
| PartRides | PartRide |
| PartRideApprovals | PartRideApproval |
| PartRideComments | PartRideComment |
| PartRideDisputes | PartRideDispute |
| PartRideFiles | PartRideFile |
| Rates | Rate |
| Surcharges | Surcharge |
| Units | Unit |
| HoursOptions | HoursOption |
| HoursCodes | HoursCode |
| DriverCompensationSettings | DriverCompensationSettings |
| Caos | Cao |
| VacationRights | VacationRight |
| EmployeeContracts | EmployeeContract |
| PeriodApprovals | PeriodApproval |
| WeekApprovals | WeekApproval |
| ClientCapacityTemplates | ClientCapacityTemplate |
| RideDriverAssignments | RideDriverAssignment |
| RideDriverExecutions | RideDriverExecution |
| RideDriverExecutionFiles | RideDriverExecutionFile |
| RideDriverExecutionComments | RideDriverExecutionComment |
| RideDriverExecutionDisputes | RideDriverExecutionDispute |
| RideDriverExecutionDisputeComments | RideDriverExecutionDisputeComment |
| RidePeriodApprovals | RidePeriodApproval |
| DriverDailyAvailabilities | DriverDailyAvailability |
| TruckDailyAvailabilities | TruckDailyAvailability |
| CAOPayScales | CAOPayScale |
| CAOVacationDays | CAOVacationDays |
| DriverContractVersions | DriverContractVersion |

---

## Global Query Filters (OnModelCreating)

Filters applied automatically to all queries. Use `IgnoreQueryFilters()` to bypass.

| Entity | Filter |
|--------|--------|
| Company | `!IsDeleted && IsApproved` |
| Client | `!IsDeleted && IsApproved` |
| ApplicationUser | `!IsDeleted && IsApproved` |
| Driver | `!IsDeleted` |
| ContactPerson | `!IsDeleted` |
| Car | `!Company.IsDeleted` |
| Charter | `!Client.IsDeleted && !Company.IsDeleted` |
| Rate | `!Client.IsDeleted && !Company.IsDeleted` |
| Surcharge | `!Client.IsDeleted && !Company.IsDeleted` |

---

## Key Relationships (OnModelCreating)

- **Company** → Clients (1:M), Drivers (1:M), CarUsedByCompany (M:M), DriverUsedByCompany (M:M)
- **Client** → Company (M:1)
- **Driver** → Company (M:1), Car (1:1 optional), DriverCompensationSettings (1:1)
- **Car** → Company (M:1), CarUsedByCompany (M:M)
- **Ride** → Company, Client, Car (Truck), RideDriverAssignment, RideDriverExecution, PartRides
- **PartRide** → Ride, Client, Driver, Car, PartRideApproval, PartRideDispute
- **ApplicationUser** → Driver (1:1), ContactPerson (1:1)
- **ContactPerson** → ContactPersonClientCompany (M:M with Company/Client)

---

## Migrations

```bash
cd TruckManagement
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

Migrations are in `TruckManagement/Migrations/`. Designer files are auto-generated.

---

## Seeding (DatabaseSeeder)

**Runs at startup** (`Program.cs`), before first request.

1. `dbContext.Database.MigrateAsync()` – Apply pending migrations
2. `CAODataSeeder.SeedCAODataAsync` – CAO pay scales, vacation days
3. Seed roles: driver, employer, customer, customerAdmin, customerAccountant, globalAdmin
4. (Optional, commented) Seed admin user, test company, etc.

**To add seed data**: Edit `Data/DatabaseSeeder.cs` or `Data/Seeding/` helpers. Use `IgnoreQueryFilters()` when checking existence if entity has filters.
