# VervoerManager – Auth Flow

## Overview

- **Mechanism**: JWT Bearer
- **No refresh token**: Single JWT; frontend stores it and sends it with each request
- **Token lifetime**: 6 months from issue

---

## Login Flow

1. **Frontend** `POST /login` with `{ email, password }`
2. **Backend** validates credentials → generates JWT with claims:
   - `sub` (user id)
   - `email`
   - `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` (per role)
3. **Response**: `{ isSuccess, statusCode, data: { token }, errors }`
4. **Frontend** saves:
   - `localStorage.setItem('authToken', token)`
   - `document.cookie = "auth=...; path=/; SameSite=Lax; Secure"`
5. **Frontend** calls `GET /users/me` to load user profile (roles, company, driver/contactPerson info)

---

## Protected Requests

- **Header**: `Authorization: Bearer <token>`
- **Middleware**: Attaches `[Authorize]` or `[Authorize(Roles = "...")]` to endpoints
- **401**: Missing or invalid token → frontend should redirect to login
- **403**: Valid token but insufficient role

---

## Session Check on App Load

1. Read `localStorage.getItem('authToken')`
2. If present: call `GET /users/me`
3. Success → set user, `isAuthenticated = true`
4. Failure (401) → clear token, redirect to login

---

## Logout

- `localStorage.removeItem('authToken')`
- Clear `auth` cookie
- No server call

---

## Password Reset

1. **Frontend** `POST /forgotpassword` with `{ email }`
2. **Backend** sends email with reset link: `{frontendUrl}?email=...&token=...`
3. **User** clicks link → Frontend `POST /reset-password-token` with `{ email, token, newPassword, confirmPassword }`
4. **Backend** validates token and updates password

---

## Roles (from JWT)

- `globalAdmin` – Full access
- `customerAdmin` – Company/client admin
- `customerAccountant` – Accountant
- `employer` – Company management
- `customer` – Client view
- `driver` – Driver dashboard
- `contactPerson` – Contact person (often combined with customerAdmin etc.)
- `planner` – Planning

---

## JWT Config (backend)

- **Config section**: `JwtSettings` in appsettings
- **SecretKey** – HMAC-SHA256 signing key
- **Issuer**, **Audience** – Validated on each request
- **ValidateLifetime**: true (token expiry enforced)
