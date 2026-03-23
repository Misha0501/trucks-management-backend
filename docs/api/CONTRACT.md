# VervoerManager – API Contract & Common DTOs

## Response Envelope

All API responses (except login) use this wrapper. Backend: `ApiResponse<T>` in `TruckManagement/Models/ApiResponse.cs`, `ApiResponseFactory` in `Helpers/ApiResponseFactory.cs`.

```json
{
  "isSuccess": true,
  "statusCode": 200,
  "data": { ... },
  "errors": null
}
```

**On error:**
```json
{
  "isSuccess": false,
  "statusCode": 400,
  "data": null,
  "errors": ["Error message 1", "Error message 2"]
}
```

---

## Login Response

`POST /login` returns the same envelope with `data: { token }`.

---

## Pagination

**Query params:** `pageNumber` (1-based), `pageSize` (default 100–1000)

**Response shape** (varies by endpoint): `totalCount`/`totalClients`, `totalPages`, `pageNumber`, `pageSize`, `data` (or `Items`).

---

## Common DTOs (shapes)

- **IDs**: All entity IDs are GUIDs (UUIDs)
- **Paginated**: `{ totalCount, totalPages, pageNumber, pageSize, data }`
- **Company, Client, Driver, Ride**: See frontend types or DTOs in `TruckManagement/DTOs/`

---

## Binary Responses

- **File download**: Returns file stream (e.g. `/car-files/{id}`)
- **File upload**: `multipart/form-data`. Returns `ApiResponse<...>` with metadata.

---

## HTTP Status Codes

| Code | Meaning |
|------|---------|
| 200 | Success |
| 400 | Bad request |
| 401 | Unauthorized |
| 403 | Forbidden |
| 404 | Not found |

---

## Headers

**Request:** `Authorization: Bearer <token>`, `Accept-Language: en|nl|bg`, `Content-Type: application/json`
