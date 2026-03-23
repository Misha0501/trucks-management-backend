# Companies Feature

## Purpose

Manage companies (employers): CRUD, approve pending companies. Companies own drivers, clients, cars, rides. Base entity for multi-tenant structure.

## Status

- [x] Implemented

## Frontend

- **Routes**: `/[locale]/companies`, `/[locale]/companies/create`, `/[locale]/companies/[id]`, `/[locale]/companies/edit`, `/[locale]/companies/pending`
- **Key pages**: `app/[locale]/companies/page.tsx`, `app/[locale]/companies/create/page.tsx`, `app/[locale]/companies/[id]/page.tsx`, `app/[locale]/companies/edit/page.tsx`, `app/[locale]/companies/pending/page.tsx`
- **Hooks**: `useCompanies`, `useUpdateCompany`, `useDeleteCompany`, `useApproveCompany`

## Backend

- **Endpoints**: `GET/POST /companies`, `GET/PUT/DELETE /companies/{id}`, `PUT /companies/{id}/approve`, `GET /companies/pending`
- **Key services**: None specific
- **Key entities**: Company, Client, Driver, Car, Ride, CarUsedByCompany, DriverUsedByCompany

## Key types / DTOs

- `Company`, `CompaniesResponse`, `EditCompanyResponse`, `DeleteCompanyResponse`

## Flows

1. **Create company**: Add company (pending) → Global admin approves → Company active
2. **Edit company**: Update name, address, Kvk, Btw, etc.
3. **Drivers/Clients**: Companies own drivers and clients; filter by company in lists

## Related

- [CLIENTS.md](CLIENTS.md), [DRIVERS.md](DRIVERS.md)
- Requirements: R23 (Company Linking)
