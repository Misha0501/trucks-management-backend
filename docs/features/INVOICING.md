# Invoicing Feature

## Purpose

Driver invoices: generate weekly invoices (PDF), driver week signing flow, period signing. Reports: ride executions, driver week/period PDFs. Weeks to submit management.

## Status

- [x] Implemented (driver invoices)

## Frontend

- **Routes**: `/[locale]/weeks-to-submit`, `/[locale]/weeks/sign/[key]`, `/[locale]/weeks/signed/[key]`, `/[locale]/weeks/signed/[key]/invoice`, `/[locale]/periods/driver/current`, `/[locale]/periods/driver/pending`, `/[locale]/periods/driver/archived`, `/[locale]/periods/driver/[periodKey]`, `/[locale]/reports`
- **Key pages**: `app/[locale]/weeks-to-submit/page.tsx`, `app/[locale]/weeks/sign/[key]/page.tsx`, `app/[locale]/weeks/signed/[key]/invoice/page.tsx`, `app/[locale]/periods/driver/current/page.tsx`, `app/[locale]/reports/page.tsx`
- **Hooks**: `useRideWeeksToSubmit`, `useWeeksToSubmit`, `useWeekToSubmitDetail`, `useWeekStatus`, `useSignDriverWeek`, `useGenerateInvoice`, `useCurrentDriverPeriod`, `usePendingDriverPeriods`, `useArchivedDriverPeriods`, `useDriverPeriodDetail`

## Backend

- **Endpoints**: `GET /rides/weeks-to-submit`, `GET /rides/week/{weekStartDate}`, `PUT /rides/week/{weekStartDate}/submit`, `GET /rides/periods/driver/pending`, `GET /rides/drivers/week/details`, `PUT /rides/weeks-to-submit/{id}/sign`, `PUT /rides/weeks-to-submit/{id}/allow-driver`, `POST /drivers/{driverId}/weeks/{weekNumber}/invoice`, `GET /rides/periods/{year}/{periodNumber}`, `PUT /rides/period/{year}/{periodNumber}/sign-driver`, `GET /rides/period/{year}/{periodNumber}/pdf`, `GET /reports/ride-executions`, `GET /reports/driver/{driverId}/week/{year}/{weekNumber}/pdf`, `GET /reports/driver/{driverId}/period/{year}/{periodNumber}/pdf`, `GET /weeks-to-submit`, `GET /weeks-to-submit/{id}`, `PUT /weeks-to-submit/{id}`
- **Key services**: `DriverInvoiceService`, `DriverInvoicePdfBuilder`
- **Key entities**: WeekApproval, PeriodApproval, RideDriverExecution, PartRide

## Key types / DTOs

- `DriverWeekSummary`, `WeekDetails`, `WeekSubmissionResult`, `DriverWeekDetails`, `SignDriverWeekResponse`, `WeekToSubmitDetail`, `ArchivedPeriodPage`, `CurrentDriverPeriod`

## Flows

1. **Week to submit**: Admin creates week → Driver submits hours → Admin reviews → Driver signs
2. **Invoice**: After sign → Generate PDF invoice for driver
3. **Period**: Driver signs period → PDF generated
4. **Reports**: Export ride executions, driver week/period PDFs

## Related

- [DRIVERS.md](DRIVERS.md), [RIDES.md](RIDES.md)
- Requirements: R34 (Payroll), R37 (Invoicing)
