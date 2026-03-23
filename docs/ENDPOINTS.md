# VervoerManager – API Endpoints Reference

Base URL: `https://api.vervoermanager.nl` (production) or `NEXT_PUBLIC_API_BASE_URL` (dev).

All endpoints return JSON wrapped in `ApiResponse<T>` unless noted. See [api/CONTRACT.md](CONTRACT.md) for response format.

---

## Auth (no Bearer required)

| Method | Path | Description |
|--------|------|-------------|
| POST | `/login` | Login. Returns `{ token }` |
| POST | `/register` | Register user (globalAdmin only) |
| POST | `/forgotpassword` | Request password reset email |
| POST | `/reset-password-token` | Reset password with token |

---

## Users & Profile

| Method | Path | Description |
|--------|------|-------------|
| GET | `/users/me` | Current user profile (roles, driver/contactPerson info) |
| GET | `/users` | List users (paginated) |
| GET | `/users/{id}` | User detail |
| PUT | `/users/{id}/basic` | Update user basic info |
| PUT | `/users/{id}/driver` | Update driver assignment (company, car) |
| PUT | `/users/{id}/contact-person` | Update contact person associations |
| POST | `/users/change-password` | Change password |
| DELETE | `/contactpersons/{id}` | Delete contact person |
| DELETE | `/drivers/{id}` | Delete driver |
| GET | `/customeradmins` | List customer admins |

---

## Driver Compensations

| Method | Path | Description |
|--------|------|-------------|
| GET | `/users/{aspUserId}/driver/compensations` | Get driver compensation settings |
| PUT | `/users/{aspUserId}/driver/compensations` | Update driver compensation settings |

---

## Companies

| Method | Path | Description |
|--------|------|-------------|
| GET | `/companies` | List companies (paginated) |
| GET | `/companies/{id}` | Company detail |
| POST | `/companies` | Create company |
| PUT | `/companies/{id}` | Update company |
| DELETE | `/companies/{id}` | Delete company |
| PUT | `/companies/{id}/approve` | Approve company |
| GET | `/companies/pending` | Pending companies |

---

## Clients

| Method | Path | Description |
|--------|------|-------------|
| GET | `/clients` | List clients (paginated) |
| GET | `/clients/{id}` | Client detail |
| POST | `/clients` | Create client |
| PUT | `/clients/{id}` | Update client |
| DELETE | `/clients/{id}` | Delete client |
| PUT | `/clients/{id}/approve` | Approve client |
| GET | `/clients/pending` | Pending clients |

---

## Drivers

| Method | Path | Description |
|--------|------|-------------|
| GET | `/drivers` | List drivers (paginated) |
| POST | `/drivers/create-with-contract` | Create driver with contract |
| GET | `/drivers/{driverId}/with-contract` | Driver with contract detail |
| PUT | `/drivers/{driverId}/with-contract` | Update driver with contract |
| DELETE | `/drivers/{driverId}/with-contract` | Delete driver |
| POST | `/drivers/{driverId}/contracts/regenerate` | Regenerate contract |
| GET | `/drivers/{driverId}/contracts` | List contract versions |
| GET | `/drivers/{driverId}/contracts/latest` | Latest contract version |
| GET | `/drivers/{driverId}/contracts/{versionId}` | Contract version detail |
| GET | `/drivers/{driverId}/contracts/{versionId}/download` | Download contract PDF |
| GET | `/drivers/periods/current` | Current driver period |
| GET | `/drivers/periods/pending` | Pending periods |
| GET | `/drivers/periods/archived` | Archived periods |
| GET | `/drivers/periods/{periodKey}` | Period detail |
| GET | `/drivers/week/details` | Driver week details |
| POST | `/drivers/week/sign` | Sign driver week |
| POST | `/drivers/{driverId}/weeks/{weekNumber}/invoice` | Generate driver invoice |

---

## Contact Persons

| Method | Path | Description |
|--------|------|-------------|
| GET | `/contactpersons` | List contact persons |

---

## Cars

| Method | Path | Description |
|--------|------|-------------|
| GET | `/cars` | List cars (paginated) |
| GET | `/cars/{id}` | Car detail |
| POST | `/cars` | Create car |
| PUT | `/cars/{id}` | Update car |
| DELETE | `/cars/{id}` | Delete car |
| GET | `/car-files/{fileId}` | Download car file (Blob) |

---

## Charters

| Method | Path | Description |
|--------|------|-------------|
| GET | `/charters` | List charters |
| GET | `/charters/{id}` | Charter detail |
| POST | `/charters` | Create charter |
| PUT | `/charters/{id}` | Update charter |
| DELETE | `/charters/{id}` | Delete charter |

---

## Rides

| Method | Path | Description |
|--------|------|-------------|
| GET | `/rides` | List rides (paginated) |
| GET | `/rides/{id}` | Ride detail |
| POST | `/rides` | Create ride |
| PUT | `/rides/{id}` | Update ride |
| DELETE | `/rides/{id}` | Delete ride |
| PUT | `/rides/{id}/assign` | Assign driver to ride |
| POST | `/rides/{id}/second-driver` | Add second driver |
| DELETE | `/rides/{id}/second-driver` | Remove second driver |
| PUT | `/rides/{id}/hours` | Update ride hours |
| PUT | `/rides/{id}/details` | Update ride details |
| PUT | `/rides/{id}/trip-number` | Update trip number |
| PUT | `/rides/{id}/my-execution` | Driver: update own execution |
| GET | `/rides/{id}/my-execution` | Driver: get own execution |
| GET | `/rides/{id}/executions` | List ride executions |
| GET | `/rides/my-assigned` | Driver: my assigned rides |
| DELETE | `/rides/{id}/my-execution` | Driver: delete own execution |
| PUT | `/rides/{id}/executions/{driverId}/approve` | Approve execution |
| PUT | `/rides/{id}/executions/bulk-approve` | Bulk approve executions |
| PUT | `/rides/{id}/executions/{driverId}/reject` | Reject execution |
| GET | `/rides/pending-approval` | Rides pending approval |
| POST | `/rides/{id}/my-execution/files` | Upload execution file |
| GET | `/rides/{id}/my-execution/files` | List execution files |
| GET | `/rides/{id}/my-execution/files/{fileId}` | Download execution file |
| DELETE | `/rides/{id}/my-execution/files/{fileId}` | Delete execution file |
| POST | `/rides/{id}/my-execution/disputes` | Create execution dispute |
| GET | `/rides/{id}/my-execution/disputes` | List execution disputes |
| POST | `/execution-disputes/{id}/comments` | Add dispute comment |
| PUT | `/execution-disputes/{id}/close` | Close execution dispute |
| POST | `/rides/{id}/executions/{driverId}/comments` | Add execution comment |
| GET | `/rides/{id}/executions/{driverId}/comments` | List execution comments |

---

## Ride Planning

| Method | Path | Description |
|--------|------|-------------|
| GET | `/weekly-planning/preview` | Weekly planning preview |
| POST | `/weekly-planning/generate` | Generate weekly rides |
| GET | `/weekly-planning/rides` | Weekly planning rides |
| GET | `/daily-planning/rides` | Daily planning rides |
| GET | `/daily-planning/available-dates` | Available dates |
| GET | `/rides/periods/{year}/{periodNumber}` | Ride period |
| PUT | `/rides/period/{year}/{periodNumber}/sign-driver` | Sign period (driver) |
| GET | `/rides/period/{year}/{periodNumber}/pdf` | Period PDF |
| GET | `/rides/weeks-to-submit` | Weeks to submit |
| GET | `/rides/week/{weekStartDate}` | Week detail |
| PUT | `/rides/week/{weekStartDate}/submit` | Submit week |
| GET | `/rides/periods/driver/pending` | Driver pending periods |
| GET | `/rides/drivers/week/details` | Driver week details |
| PUT | `/rides/weeks-to-submit/{id}/sign` | Sign week to submit |
| PUT | `/rides/weeks-to-submit/{id}/allow-driver` | Allow driver for week |

---

## Part Rides

| Method | Path | Description |
|--------|------|-------------|
| GET | `/partrides` | List part rides |
| GET | `/partrides/{id}` | Part ride detail |
| POST | `/partrides` | Create part ride |
| PUT | `/partrides/{id}` | Update part ride |
| DELETE | `/partrides/{id}` | Delete part ride |
| GET | `/partrides/{id}/disputes` | Part ride disputes |
| POST | `/partrides/{id}/disputes` | Create dispute |
| POST | `/partrides/{id}/approve` | Approve part ride |
| POST | `/partrides/{id}/reject` | Reject part ride |
| GET | `/partride-files/{fileId}` | Download part ride file (Blob) |

---

## Disputes (Part Ride)

| Method | Path | Description |
|--------|------|-------------|
| GET | `/disputes` | List disputes |
| GET | `/disputes/{id}` | Dispute detail |
| PUT | `/disputes/{id}` | Update dispute |
| DELETE | `/disputes/{id}` | Delete dispute |
| POST | `/disputes/{id}/comments` | Add comment |
| POST | `/disputes/{id}/accept` | Accept dispute |
| POST | `/disputes/{id}/close` | Close dispute |

---

## Rates & Surcharges

| Method | Path | Description |
|--------|------|-------------|
| GET | `/rates/{clientId}` | List rates for client |
| GET | `/rates/detail/{id}` | Rate detail |
| POST | `/rates` | Create rate |
| PUT | `/rates/{id}` | Update rate |
| DELETE | `/rates/{id}` | Delete rate |
| GET | `/surcharges/{clientId}` | List surcharges for client |
| GET | `/surcharges/detail/{id}` | Surcharge detail |
| POST | `/surcharges` | Create surcharge |
| PUT | `/surcharges/edit/{id}` | Update surcharge |
| DELETE | `/surcharges/{id}` | Delete surcharge |

---

## Capacity & Availability

| Method | Path | Description |
|--------|------|-------------|
| GET | `/capacity-templates` | List capacity templates |
| POST | `/capacity-templates` | Create capacity template |
| PUT | `/capacity-templates/{id}` | Update capacity template |
| DELETE | `/capacity-templates/{id}` | Delete capacity template |
| GET | `/availability/week/{weekStartDate}` | Weekly availability |
| PUT | `/availability/driver/{driverId}/bulk` | Bulk update driver availability |
| PUT | `/availability/truck/{truckId}/bulk` | Bulk update truck availability |

---

## Employee Contracts

| Method | Path | Description |
|--------|------|-------------|
| GET | `/employee-contracts` | List contracts |
| GET | `/employee-contracts/{id}` | Contract detail |
| GET | `/employee-contracts/{id}/public` | Public contract (signing) |
| POST | `/employee-contracts` | Create contract |
| PUT | `/employee-contracts/{id}` | Update contract |
| DELETE | `/employee-contracts/{id}` | Delete contract |
| POST | `/employee-contracts/sign` | Sign contract |
| GET | `/employee-contracts/{id}/download` | Download signed contract |
| POST | `/employee-contracts/send-sign-mail` | Send sign email |

---

## Reports

| Method | Path | Description |
|--------|------|-------------|
| GET | `/reports/ride-executions` | Ride executions report |
| GET | `/reports/driver/{driverId}/week/{year}/{weekNumber}/pdf` | Driver week PDF |
| GET | `/reports/driver/{driverId}/period/{year}/{periodNumber}/pdf` | Driver period PDF |

---

## Weeks to Submit / Week Detail

| Method | Path | Description |
|--------|------|-------------|
| GET | `/weeks-to-submit` | List weeks to submit |
| GET | `/weeks-to-submit/{id}` | Week to submit detail |
| PUT | `/weeks-to-submit/{id}` | Update week to submit |

---

## Reference Data

| Method | Path | Description |
|--------|------|-------------|
| GET | `/roles` | List roles |
| GET | `/units` | List units |
| GET | `/units/{id}` | Unit detail |
| POST | `/units` | Create unit |
| PUT | `/units/{id}` | Update unit |
| DELETE | `/units/{id}` | Delete unit |
| GET | `/hourscodes` | List hours codes |
| GET | `/hourscodes/{id}` | Hours code detail |
| GET | `/hoursoptions` | List hours options |
| GET | `/hoursoptions/{id}` | Hours option detail |

---

## File Uploads

| Method | Path | Description |
|--------|------|-------------|
| POST | `/temporary-uploads` | Temporary file upload (multipart) |

---

## Driver Files

| Method | Path | Description |
|--------|------|-------------|
| GET | `/driver-files/{fileId}` | Download driver file (Blob) |

---

## Telegram

| Method | Path | Description |
|--------|------|-------------|
| POST | `/telegram/webhook` | Telegram webhook (no auth) |
| GET | `/drivers/{id}/telegram/registration-link` | Get Telegram registration link |
| DELETE | `/drivers/{id}/telegram` | Disconnect Telegram |
| POST | `/telegram/test` | Test Telegram |
