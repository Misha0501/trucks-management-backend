# VervoerManager – Project & Domain Context

**For AI assistants**: Domain concepts, Dutch terms, and business context.

## What is VervoerManager?
Truck/transport management system for Dutch logistics companies. Manages drivers, vehicles, clients, ride planning, invoicing, and driver payroll.

## Core Domain Entities

| Entity | Table/Entity | Description |
|--------|--------------|-------------|
| **Company** | Companies | Employer organization |
| **Client** | Clients | Customer company (receives rides) |
| **Driver** | Drivers | Employee. Links to ApplicationUser, EmployeeContract |
| **Car** | Cars | Truck/vehicle. CarUsedByCompany for multi-company |
| **Ride** | Rides | Planned trip. RideDriverAssignments for drivers |
| **PartRide** | PartRides | Partial ride. PartRideDispute, PartRideApproval |
| **ContactPerson** | ContactPersons | Client admin |
| **EmployeeContract** | EmployeeContracts | Driver contract (CAO, pay scale, etc.) |

## Dutch Terms (Glossary)
- **CAO**: Collective labor agreement
- **ZZP**: Self-employed
- **Inleen**: Secondment
- **VOG**: Certificate of conduct
- **APK**: Vehicle inspection
- **ADV**: Compensation hours
- **Verkoop/Inkoop**: Sales/purchase rates

## User Roles (ApplicationRole)
globalAdmin, customerAdmin, employer, planner, driver, contactPerson

## Requirements
See frontend repo: `plans/requirments/Phase 1 Client Requirements final.md`, `Phase 1 Business Requirements final.md`
