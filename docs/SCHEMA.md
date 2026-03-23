# VervoerManager – Database Schema (Sketch)

PostgreSQL, managed by EF Core. Entities in backend `TruckManagement/Entities/`.

---

## Core Entities & Relationships

```
ApplicationUser (ASP.NET Identity)
    │
    ├── Driver (1:1) ── CompanyId ──► Company
    │       │              │
    │       ├── CarId ─────► Car ──► CompanyId ──► Company
    │       ├── DriverUsedByCompany (M:M) ──► Company
    │       └── DriverCompensationSettings
    │
    └── ContactPerson (1:1)
            └── ContactPersonClientCompany (M:M) ──► Company, Client

Company
    ├── Clients (1:M)
    ├── Drivers (1:M)
    ├── Cars (via CarUsedByCompany)
    └── Rides (1:M)

Client ── CompanyId ──► Company
    └── PartRides (1:M)

Ride ── CompanyId, ClientId, TruckId
    ├── RideDriverAssignment
    ├── RideDriverExecution
    └── PartRides (1:M)

PartRide ── RideId, ClientId, DriverId, CarId, CompanyId
    ├── PartRideApproval
    ├── PartRideDispute
    └── PartRideFile

Car ── CompanyId ──► Company
    ├── Driver (1:1, optional)
    └── CarUsedByCompany (M:M) ──► Company
```

---

## Entity Summary

| Entity | Key fields | Notes |
|--------|------------|-------|
| **Company** | Id, Name, Kvk, Btw | Employer. IsApproved, IsDeleted |
| **Client** | Id, Name, CompanyId | Customer. IsApproved, IsDeleted |
| **Driver** | Id, AspNetUserId, CompanyId, CarId | Links to ApplicationUser |
| **Car** | Id, LicensePlate, CompanyId | Truck. Many-to-many with Company |
| **Ride** | Id, CompanyId, ClientId, PlannedDate, TruckId | Planned trip |
| **PartRide** | Id, RideId, DriverId, CarId, ClientId, Date | Status: PendingAdmin, Dispute, Accepted, Rejected |
| **ContactPerson** | Id, AspNetUserId | Links to ApplicationUser |
| **EmployeeContract** | Id, DriverId, CompanyId | Contract details, signing |
| **Charter** | Id, ... | Long-term vehicle charter |
| **Rate** | Id, ClientId | Pricing |
| **Surcharge** | Id, ClientId | Surcharges |
| **Unit** | Id | Units of measure |

---

## Junction / Supporting Tables

| Table | Purpose |
|-------|---------|
| **ContactPersonClientCompany** | Links ContactPerson to Company and/or Client |
| **CarUsedByCompany** | Car can be used by multiple companies |
| **DriverUsedByCompany** | Driver can work for multiple companies |
| **RideDriverAssignment** | Driver(s) assigned to ride |
| **RideDriverExecution** | Driver execution (hours, status) |
| **PartRideApproval** | Client approval for part ride |
| **PartRideDispute** | Dispute on part ride |
| **ClientCapacityTemplate** | Recurring capacity per client (Mon–Sun) |
| **DriverCompensationSettings** | Per-driver compensation |
| **DriverContractVersion** | Contract versions |

---

## Soft Delete

`IsDeleted` on Company, Client, Driver. Filter in queries.
