# Part Rides Feature

## Purpose

Manage part rides (deelritten): partial ride records with client approval flow. Create part rides, approve/reject, handle disputes. Part rides link to rides and clients; used for invoicing and hours tracking.

## Status

- [x] Implemented

## Frontend

- **Routes**: `/[locale]/partrides`, `/[locale]/partrides/create`, `/[locale]/partrides/edit`, `/[locale]/contact-person/partrides/[id]`, `/[locale]/driver/partrides/[id]`, `/[locale]/contact-person/disputes`, `/[locale]/contact-person/disputes/[id]`, `/[locale]/driver/disputes`, `/[locale]/driver/disputes/[id]`
- **Key pages**: `app/[locale]/partrides/page.tsx`, `app/[locale]/partrides/create/page.tsx`, `app/[locale]/partrides/edit/page.tsx`, `app/[locale]/contact-person/partrides/[id]/page.tsx`, `app/[locale]/driver/disputes/page.tsx`
- **Hooks**: `usePartRides`, `useCreatePartRide`, `usePartRideApprovals`, `usePartRideDisputes`, `useDisputeById`, `useAddDisputeComment`, `useUpdateDispute`, `useCloseDispute`, `useDeleteDispute`, `usePartRideApprovals`, `useDownloadPartRideFile`

## Backend

- **Endpoints**: `GET/POST /partrides`, `GET/PUT/DELETE /partrides/{id}`, `GET /partrides/{id}/disputes`, `POST /partrides/{id}/disputes`, `POST /partrides/{id}/approve`, `POST /partrides/{id}/reject`, `GET /disputes`, `GET /disputes/{id}`, `PUT /disputes/{id}`, `DELETE /disputes/{id}`, `POST /disputes/{id}/comments`, `POST /disputes/{id}/accept`, `POST /disputes/{id}/close`, `GET /partride-files/{fileId}`
- **Key services**: None specific
- **Key entities**: PartRide, PartRideApproval, PartRideDispute, PartRideDisputeComment, PartRideFile

## Key types / DTOs

- `PartRide`, `PartRidesResponse`, `CreatePartRideResponse`, `PartRideDisputesResponse`, `DisputeDetail`, `AddCommentResponse`, `UpdateDisputeResponse`

## Flows

1. **Create part ride**: Select client, driver, car, date, times → Submit
2. **Approval**: Client/admin approves or rejects part ride
3. **Dispute**: If driver disagrees → Create dispute → Comments → Accept or close

## Related

- [RIDES.md](RIDES.md), [CLIENTS.md](CLIENTS.md), [DRIVERS.md](DRIVERS.md)
- Requirements: R6, R8
