# Drivers Feature

## Purpose

Manage drivers: create, edit, view, assign to companies/cars. Generate and sign employment contracts. Driver compensation settings. Driver week signing and invoice generation. Telegram notifications.

## Status

- [x] Implemented

## Frontend

- **Routes**: `/[locale]/drivers`, `/[locale]/drivers/create`, `/[locale]/drivers/[id]`, `/[locale]/drivers/edit/[id]`, `/[locale]/users/edit/[id]/driver`, `/[locale]/users/edit/[id]/driver/compensations`
- **Key pages**: `app/[locale]/drivers/page.tsx`, `app/[locale]/drivers/create/page.tsx`, `app/[locale]/drivers/[id]/page.tsx`, `app/[locale]/drivers/edit/[id]/page.tsx`
- **Hooks**: `useDrivers`, `useCreateDriver`, `useUpdateDriver`, `useDriverWithContract`, `useDriverCompensations`, `useDriverContracts`, `useDriverFiles`, `useAssignCarToDriver`, `useDriverWeekDetails`, `useSignDriverWeek`, `useGenerateInvoice`

## Backend

- **Endpoints**: `GET/POST /drivers`, `GET/PUT/DELETE /drivers/{id}`, `GET /drivers/{driverId}/with-contract`, `PUT /drivers/{driverId}/with-contract`, `POST /drivers/create-with-contract`, `GET /drivers/{driverId}/contracts`, `GET /drivers/{driverId}/contracts/latest`, `GET /drivers/{driverId}/contracts/{versionId}/download`, `POST /drivers/{driverId}/contracts/regenerate`, `GET /drivers/periods/*`, `GET /drivers/week/details`, `POST /drivers/week/sign`, `POST /drivers/{driverId}/weeks/{weekNumber}/invoice`, `GET/PUT /users/{aspUserId}/driver/compensations`, `GET /drivers/{id}/telegram/registration-link`
- **Key services**: `DriverCompensationService`, `DriverContractService`, `DriverContractPdfBuilder`, `DriverInvoiceService`, `TelegramNotificationService`
- **Key entities**: Driver, DriverCompensationSettings, DriverContractVersion, EmployeeContract, DriverFile

## Key types / DTOs

- `DriverWithContract`, `DriverSummary`, `CreateDriverRequest`, `DriverCompensationSettings`, `DriverWeekDetails`, `DriverInvoiceResponse`

## Flows

1. **Create driver**: Create driver with contract → Assign company → Generate contract PDF → Send sign email or share link
2. **Edit driver**: Update profile, assign company/car, edit compensations
3. **Driver week**: Driver submits hours → Admin reviews → Driver signs → Invoice generated
4. **Telegram**: Admin generates registration link → Driver connects bot → Notifications enabled

## Related

- [CLIENTS.md](CLIENTS.md), [RIDES.md](RIDES.md), [INVOICING.md](INVOICING.md)
- Requirements: R21 (Onboarding), R23 (Potential Driver), R24–R25 (Contracts)
