# VervoerManager – Environment Variables Reference

## Frontend

| Variable | Required | Description |
|----------|----------|-------------|
| `NEXT_PUBLIC_API_BASE_URL` | Yes | API base URL. Baked at build time. |
| | | Dev: `http://localhost:5000` or `https://api.vervoermanager.nl` |
| | | Prod: `https://api.vervoermanager.nl` |

**File:** `.env.local` (dev), or set in build command (prod).

---

## Backend

### Database (PostgreSQL)

| Variable | Required | Description |
|----------|----------|-------------|
| `CONNECTION_STRING` | Yes | EF Core connection string |
| | | Example: `Host=postgresdb;Port=5432;Database=TruckManagement;Username=postgres;Password=...;Include Error Detail=true` |
| `POSTGRES_USER` | Yes (compose) | Postgres username |
| `POSTGRES_PASSWORD` | Yes (compose) | Postgres password |
| `POSTGRES_DB` | Yes (compose) | Database name (e.g. TruckManagement) |
| `POSTGRES_PORT` | No | Host port for Postgres (default 5460) |

### JWT (in appsettings or env)

| Config Key | Required | Description |
|------------|----------|-------------|
| `JwtSettings:SecretKey` | Yes | HMAC-SHA256 secret (min 32 chars) |
| `JwtSettings:Issuer` | Yes | Token issuer |
| `JwtSettings:Audience` | Yes | Token audience |

### SMTP (email)

| Variable / Config | Required | Description |
|-------------------|----------|-------------|
| `Smtp__Host` / `SMTP_HOST` | Yes | SMTP server |
| `Smtp__Port` / `SMTP_PORT` | Yes | Port (e.g. 587) |
| `Smtp__Username` / `SMTP_USERNAME` | Yes | SMTP username |
| `Smtp__Password` / `SMTP_PASSWORD` | Yes | SMTP password |
| `Smtp__FromAddress` / `SMTP_FROM_ADDRESS` | Yes | From address |

### Frontend URLs

| Variable | Required | Description |
|----------|----------|-------------|
| `FrontEnd__ResetPasswordUrl` / `FRONTEND_RESET_PASSWORD_URL` | Yes | Reset password link base (e.g. https://vervoermanager.nl/auth/reset-password-token/) |

### Storage

| Config | Required | Description |
|--------|----------|-------------|
| `Storage__BasePath` | Yes | Base path for uploads (Docker: `/app/storage`) |

### Telegram

| Variable | Required | Description |
|----------|----------|-------------|
| `Telegram__BotToken` / `TELEGRAM_BOT_TOKEN` | For notifications | Telegram bot token |
| `Telegram__BotUsername` | No | e.g. VervoerManager_Driver_Bot |

### pgAdmin (Docker only)

| Variable | Required | Description |
|----------|----------|-------------|
| `PGADMIN_DEFAULT_EMAIL` | No | pgAdmin login email |
| `PGADMIN_DEFAULT_PASSWORD` | No | pgAdmin login password |

---

## Docker Compose (.env)

Backend `compose.yaml` reads from `.env`:

```
CONNECTION_STRING=Host=postgresdb;Port=5432;Database=TruckManagement;...
POSTGRES_USER=postgres
POSTGRES_PASSWORD=...
POSTGRES_DB=TruckManagement
POSTGRES_PORT=5460

SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=...
SMTP_PASSWORD=...
SMTP_FROM_ADDRESS=...

FRONTEND_RESET_PASSWORD_URL=https://vervoermanager.nl/auth/reset-password-token/

TELEGRAM_BOT_TOKEN=...

PGADMIN_DEFAULT_EMAIL=admin@admin.com
PGADMIN_DEFAULT_PASSWORD=...
```
