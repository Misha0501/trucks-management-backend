# Rides Feature

## Purpose

Plan and manage rides (planned trips): create rides, assign trucks and drivers, execute (driver logs hours), approve executions. Weekly and daily planning. Ride assignment (primary/secondary driver), trip numbers, execution disputes.

## Status

- [x] Implemented

## Frontend

- **Routes**: `/[locale]/rides`, `/[locale]/rides/create`, `/[locale]/rides/[id]`, `/[locale]/rides/edit/[id]`, `/[locale]/planning/weekly`, `/[locale]/planning/daily`, `/[locale]/rides/executions`, `/[locale]/driver/rides`, `/[locale]/driver/rides/[id]`, `/[locale]/execution-disputes`
- **Key pages**: `app/[locale]/rides/page.tsx`, `app/[locale]/rides/create/page.tsx`, `app/[locale]/rides/[id]/page.tsx`, `app/[locale]/planning/weekly/page.tsx`, `app/[locale]/planning/daily/page.tsx`, `app/[locale]/rides/executions/page.tsx`
- **Hooks**: `useRides`, `useCreateRide`, `useEditRide`, `useDeleteRide`, `useRideDetails`, `useRideAssignment`, `useWeeklyRides`, `useWeeklyPreview`, `useDailyRides`, `useRideExecution`, `useRideExecutionReport`, `useRideExecutionApproval`, `useRideExecutionDisputes`, `useMyAssignedRides`, `useTripNumber`

## Backend

- **Endpoints**: `GET/POST /rides`, `GET/PUT/DELETE /rides/{id}`, `PUT /rides/{id}/assign`, `POST /rides/{id}/second-driver`, `DELETE /rides/{id}/second-driver`, `PUT /rides/{id}/hours`, `PUT /rides/{id}/details`, `PUT /rides/{id}/trip-number`, `GET /weekly-planning/preview`, `POST /weekly-planning/generate`, `GET /weekly-planning/rides`, `GET /daily-planning/rides`, `GET /daily-planning/available-dates`, `PUT /rides/{id}/my-execution`, `GET /rides/{id}/my-execution`, `GET /rides/{id}/executions`, `GET /rides/my-assigned`, `PUT /rides/{id}/executions/{driverId}/approve`, `PUT /rides/{id}/executions/bulk-approve`, `PUT /rides/{id}/executions/{driverId}/reject`, `POST /rides/{id}/my-execution/files`, `GET /rides/{id}/my-execution/files`, `POST /rides/{id}/my-execution/disputes`, `POST /execution-disputes/{id}/comments`, `PUT /execution-disputes/{id}/close`
- **Key services**: None specific
- **Key entities**: Ride, RideDriverAssignment, RideDriverExecution, RideDriverExecutionFile, RideDriverExecutionDispute

## Key types / DTOs

- `Ride`, `CreateRideRequest`, `RideDetailsResponse`, `WeeklyRidesData`, `DailyRidesData`, `RideDriverExecution`, `RideExecutionReportResponse`, `MyAssignedRide`

## Flows

1. **Weekly planning**: Preview capacity → Generate rides from templates → Assign trucks/drivers
2. **Daily planning**: View rides by date → Assign trucks/drivers
3. **Ride execution**: Driver logs start/end hours, uploads files → Admin approves → Execution complete
4. **Execution dispute**: Driver disputes → Comments → Admin closes

## Related

- [DRIVERS.md](DRIVERS.md), [PARTRIDES.md](PARTRIDES.md), [PLANNING.md](PLANNING.md)
- Requirements: R6 (Client Daily Capacity), R8 (Schedule View)
