# VervoerManager Backend – Development Setup

## Prerequisites
- .NET 9 SDK
- PostgreSQL 15+ (or use Docker)

## Quick Start (Local)

```bash
git clone https://github.com/Misha0501/trucks-management-backend.git
cd trucks-management-backend
dotnet restore TruckManagement.sln
cd TruckManagement
# Set ConnectionStrings__DefaultConnection in appsettings.Development.json
dotnet run
```

API: http://localhost:5000 (or port in launchSettings)

## Quick Start (Docker)

```bash
git clone https://github.com/Misha0501/trucks-management-backend.git
cd trucks-management-backend
cp .env.example .env   # set CONNECTION_STRING, POSTGRES_*, etc.
docker compose up -d postgresdb
# Run migrations
cd TruckManagement && dotnet ef database update
dotnet run
```

## Environment / Config

- **Development**: `appsettings.Development.json` – ConnectionStrings, Smtp, FrontEnd URLs
- **Production**: `.env` – All config via environment

Example connection string:
```
Host=localhost;Port=5432;Database=truckmanagement;Username=postgres;Password=...
```

## Commands

| Command | Description |
|---------|-------------|
| `dotnet run` | Run API |
| `dotnet ef migrations add <Name>` | Add migration |
| `dotnet ef database update` | Apply migrations |

## Frontend

Frontend expects API at `NEXT_PUBLIC_API_BASE_URL`. Update frontend `.env.local` to match your backend URL.
