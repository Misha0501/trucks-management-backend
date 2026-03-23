# Backend Middleware Pipeline

Order matters. Pipeline runs top-to-bottom on request, bottom-to-top on response.

---

## Pipeline Order

1. **UseRequestLocalization**
   - Sets culture from `Accept-Language` header (en, nl, bg)
   - Used by `IResourceLocalizer` for localized strings

2. **UseCors("AllowAll")**
   - Allows any origin, method, header
   - Configured in Program.cs: `AddPolicy("AllowAll", ...)`

3. **GlobalExceptionHandlingMiddleware**
   - **Catches all unhandled exceptions**
   - Returns `ApiResponse<object>` JSON: `{ isSuccess: false, statusCode, data: null, errors: [...] }`
   - **PostgreSQL exceptions** (DbUpdateException → PostgresException):
     - `23503` FK violation → 400, "foreign key constraint violated"
     - `23505` unique violation → 400, "unique constraint violated"
     - `23502` not null → 400, "required column was null"
     - `23514` check constraint → 400, "check constraint violated"
   - **Other exceptions**: 500, message in errors array

4. **DatabaseSeeder.SeedAsync** (run once at startup, not per-request)
   - Applies pending migrations
   - Seeds CAO lookup data
   - Seeds roles (driver, employer, customer, customerAdmin, customerAccountant, globalAdmin)
   - Optionally seeds test admin user (see DatabaseSeeder)

5. **MapOpenApi** (Development only)
   - Swagger/OpenAPI docs

6. **UseHttpsRedirection**
   - Redirects HTTP to HTTPS

7. **UseAuthentication**
   - JWT Bearer. Validates token on `[Authorize]` endpoints.
   - Token from `Authorization: Bearer <token>`

8. **UseAuthorization**
   - Enforces `[Authorize]`, `[Authorize(Roles = "...")]`
   - Policies: EmployeeOnly, EmployerOnly, CustomerOnly, CustomerAdminOnly, CustomerAccountantOnly, GlobalAdminOnly

9. **Endpoints**
   - All route handlers registered via MapXxxEndpoints()

---

## GlobalExceptionHandlingMiddleware Details

- **File**: `TruckManagement/Middlewares/GlobalExceptionHandlingMiddleware.cs`
- **Registration**: `app.UseMiddleware<GlobalExceptionHandlingMiddleware>();`
- **Response format**: Same `ApiResponse<T>` as normal API responses
- **Logging**: Does not log; consider adding `ILogger` if needed
