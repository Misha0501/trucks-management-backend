# Clients Feature

## Purpose

Manage clients (customers): CRUD, approve pending clients. Rates and surcharges per client. Contact persons linked to clients. Client capacity templates for planning.

## Status

- [x] Implemented

## Frontend

- **Routes**: `/[locale]/clients`, `/[locale]/clients/create`, `/[locale]/clients/[id]`, `/[locale]/clients/edit`, `/[locale]/clients/pending`, `/[locale]/rates/[clientId]`, `/[locale]/rates/create`, `/[locale]/rates/edit/[id]`, `/[locale]/rates/detail/[id]`, `/[locale]/surcharges/[clientId]`, `/[locale]/surcharges/create`, `/[locale]/surcharges/detail/[id]`, `/[locale]/surcharges/edit/[id]`, `/[locale]/contactpersons/[id]`
- **Key pages**: `app/[locale]/clients/page.tsx`, `app/[locale]/clients/create/page.tsx`, `app/[locale]/clients/[id]/page.tsx`, `app/[locale]/rates/[clientId]/page.tsx`, `app/[locale]/surcharges/[clientId]/page.tsx`
- **Hooks**: `useClients`, `useClientDetails`, `useCreateClient`, `useEditClient`, `useDeleteClient`, `usePendingClients`, `useApproveClient`, `useRates`, `useRateDetail`, `useCreateRate`, `useEditRate`, `useSurcharges`, `useSurchargeDetails`, `useCreateSurcharge`, `useEditSurcharge`, `useDeleteSurcharge`, `useContactPersons`, `useCustomerAdminDetail`, `useUpdateCustomerAdminAssociations`

## Backend

- **Endpoints**: `GET/POST /clients`, `GET/PUT/DELETE /clients/{id}`, `PUT /clients/{id}/approve`, `GET /clients/pending`, `GET /rates/{clientId}`, `GET/POST /rates`, `PUT/DELETE /rates/{id}`, `GET /rates/detail/{id}`, `GET /surcharges/{clientId}`, `GET/POST /surcharges`, `PUT /surcharges/edit/{id}`, `DELETE /surcharges/{id}`, `GET /surcharges/detail/{id}`, `GET /contactpersons`, `GET /capacity-templates`, `POST/PUT/DELETE /capacity-templates`
- **Key services**: None specific
- **Key entities**: Client, Rate, Surcharge, ContactPerson, ContactPersonClientCompany, ClientCapacityTemplate

## Key types / DTOs

- `Client`, `ClientsData`, `CreateClientResponse`, `EditClientResponse`, `RatesResponse`, `RateDetailResponse`, `SurchargesResponse`, `SurchargeDetails`, `ContactPersonsData`

## Flows

1. **Create client**: Add client (pending) → Admin approves → Client active
2. **Rates**: Define rates per client (Verkoop/Inkoop, unit, etc.)
3. **Surcharges**: Add surcharges per client (toll, tunnel, etc.)
4. **Contact persons**: Link users as contact persons to clients/companies

## Related

- [COMPANIES.md](COMPANIES.md), [PLANNING.md](PLANNING.md)
- Requirements: R6 (Client Daily Capacity), R14 (Customer Invoice)
