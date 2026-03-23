# VervoerManager – Entity Details

Extend this file when adding or significantly changing entities. See [CONTRIBUTING_DOCS.md](../CONTRIBUTING_DOCS.md).

**DbContext**: `ApplicationDbContext` in `Data/ApplicationDbContext.cs`  
**Global filters**: See [core/DATABASE.md](core/DATABASE.md)

---

## Core Entities

### Company

- **Table**: `Companies`
- **File**: `Entities/Company.cs`
- **Key fields**: Id, Name, Kvk, Btw, Address, Postcode, City, IsApproved, IsDeleted
- **Notes**: Employer organization. Must be approved (IsApproved). Soft delete (IsDeleted). Has Clients, Drivers, CarUsedByCompany, DriverUsedByCompany.

### Client

- **Table**: `Clients`
- **File**: `Entities/Client.cs`
- **Key fields**: Id, Name, CompanyId, Tav, Address, Kvk, Btw, IsApproved, IsDeleted
- **Notes**: Customer company. Belongs to one Company. Soft delete.

### Driver

- **Table**: `Drivers`
- **File**: `Entities/Driver.cs`
- **Key fields**: Id, AspNetUserId, CompanyId, CarId, IsDeleted, TelegramChatId, TelegramNotificationsEnabled
- **Notes**: Links to ApplicationUser. 1:1 with DriverCompensationSettings. Can have one Car. M:M with Company via DriverUsedByCompany.

### Car

- **Table**: `Cars`
- **File**: `Entities/Car.cs`
- **Key fields**: Id, LicensePlate, CompanyId, VehicleYear, RegistrationDate, LeasingStartDate, LeasingEndDate
- **Notes**: Truck/vehicle. M:M with Company via CarUsedByCompany. Optional 1:1 with Driver.

### Ride

- **Table**: `Rides`
- **File**: `Entities/Ride.cs`
- **Key fields**: Id, CompanyId, ClientId, PlannedDate, TruckId, TotalPlannedHours, PlannedStartTime, PlannedEndTime, RouteFromName, RouteToName, TripNumber, ExecutionCompletionStatus
- **Notes**: Planned trip. RideDriverAssignment (primary/secondary), RideDriverExecution (actual hours). PartRides.

### PartRide

- **Table**: `PartRides`
- **File**: `Entities/PartRide.cs`
- **Key fields**: Id, RideId, ClientId, DriverId, CarId, CompanyId, Date, Start, End, Status, DecimalHours, WeekNumber, PeriodNumber
- **Notes**: Status enum: PendingAdmin, Dispute, Accepted, Rejected. PartRideApproval, PartRideDispute, PartRideFile.

### ContactPerson

- **Table**: `ContactPersons`
- **File**: `Entities/ContactPerson.cs`
- **Key fields**: Id, AspNetUserId, IsDeleted
- **Notes**: Links to ApplicationUser. M:M with Company/Client via ContactPersonClientCompany.

---

## Supporting Entities

### ApplicationUser / ApplicationRole

- **Tables**: `AspNetUsers`, `AspNetRoles` (ASP.NET Identity)
- **File**: `Entities/ApplicationUser.cs`, `ApplicationRole.cs`
- **Notes**: Identity. ApplicationUser has optional Driver, ContactPerson navigation.

### EmployeeContract

- **Table**: `EmployeeContracts`
- **File**: `Entities/EmployeeContract.cs`
- **Key fields**: Id, DriverId, CompanyId, Status, AccessCode, SignedFileName, SignedAt, EmployeeFirstName, Bsn, Iban, CompensationPerMonthExclBtw
- **Notes**: Contract details. Status: Pending, Signed, etc. Used for driver contracts.

### DriverContractVersion

- **Table**: `DriverContractVersions`
- **File**: `Entities/DriverContractVersion.cs`
- **Notes**: Version history of driver contracts. Links to Driver, EmployeeContract. IsLatestVersion flag.

### DriverCompensationSettings

- **Table**: `DriverCompensationSettings`
- **File**: `Entities/DriverCompensationSettings.cs`
- **Key fields**: DriverId (PK), DriverRatePerHour, NightAllowanceRate, KilometerAllowance, Salary4Weeks
- **Notes**: 1:1 with Driver. Per-driver compensation.

### RideDriverAssignment

- **Table**: `RideDriverAssignments`
- **File**: `Entities/RideDriverAssignment.cs`
- **Notes**: Links Ride to Driver(s). IsPrimaryDriver flag.

### RideDriverExecution

- **Table**: `RideDriverExecutions`
- **File**: `Entities/RideDriverExecution.cs`
- **Key fields**: RideId, DriverId, StartTime, EndTime, TotalHours, IsApproved
- **Notes**: Actual hours logged by driver. RideDriverExecutionFile, RideDriverExecutionDispute.

### PartRideDispute / PartRideApproval

- **Files**: `Entities/PartRideDispute.cs`, `PartRideApproval.cs`
- **Notes**: Dispute flow, approval by role.

### Charter, Rate, Surcharge, Unit

- **Files**: `Entities/Charter.cs`, `Rate.cs`, `Surcharge.cs`, `Unit.cs`
- **Notes**: Charter = long-term vehicle charter. Rate/Surcharge per Client. Unit = unit of measure.

### ClientCapacityTemplate

- **Table**: `ClientCapacityTemplates`
- **File**: `Entities/ClientCapacityTemplate.cs`
- **Notes**: Recurring capacity per client (MondayTrucks, TuesdayTrucks, ...).

### DriverDailyAvailability / TruckDailyAvailability

- **Files**: `Entities/DriverDailyAvailability.cs`, `TruckDailyAvailability.cs`
- **Notes**: Availability per date. Unique (DriverId, Date) and (TruckId, Date).

### CAOPayScale / CAOVacationDays

- **Files**: `Entities/CAOPayScale.cs`, `CAOVacationDays.cs`
- **Notes**: Lookup tables for CAO (Collective Labor Agreement). Seeded by CAODataSeeder.

### WeekApproval / PeriodApproval / RidePeriodApproval

- **Files**: `Entities/WeekApproval.cs`, `PeriodApproval.cs`, `RidePeriodApproval.cs`
- **Notes**: Driver week/period signing.
