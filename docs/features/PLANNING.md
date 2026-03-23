# Planning Feature

## Purpose

Long-term capacity templates, weekly ride generation, daily planning view. Availability (driver/truck) per week. Capacity templates define recurring needs per client (Mon–Sun).

## Status

- [x] Implemented

## Frontend

- **Routes**: `/[locale]/planning/long-term`, `/[locale]/planning/weekly`, `/[locale]/planning/daily`
- **Key pages**: `app/[locale]/planning/long-term/page.tsx`, `app/[locale]/planning/weekly/page.tsx`, `app/[locale]/planning/daily/page.tsx`
- **Hooks**: `useWeeklyPreview`, `useWeeklyRides`, `useDailyRides`, `useDriversAndTrucks`, `useWeeklyAvailability`, `useCapacityTemplates` (if exists)

## Backend

- **Endpoints**: `GET /capacity-templates`, `POST/PUT/DELETE /capacity-templates`, `GET /weekly-planning/preview`, `POST /weekly-planning/generate`, `GET /weekly-planning/rides`, `GET /daily-planning/rides`, `GET /daily-planning/available-dates`, `GET /availability/week/{weekStartDate}`, `PUT /availability/driver/{driverId}/bulk`, `PUT /availability/truck/{truckId}/bulk`
- **Key entities**: ClientCapacityTemplate, Ride, DriverDailyAvailability, TruckDailyAvailability

## Key types / DTOs

- `WeeklyPreviewData`, `WeeklyRidesData`, `DailyRidesData`, `WeeklyAvailabilityData`, `DriversApiResponse`, `CarsApiResponse`

## Flows

1. **Long-term**: Create/edit capacity templates per client (trucks needed Mon–Sun)
2. **Weekly**: Preview generated rides → Generate → Assign trucks/drivers in grid
3. **Daily**: View rides by date → Assign trucks/drivers
4. **Availability**: Mark drivers/trucks unavailable for specific days

## Related

- [RIDES.md](RIDES.md), [CLIENTS.md](CLIENTS.md)
- Requirements: R6 (Client Daily Capacity), R8 (Schedule View)
