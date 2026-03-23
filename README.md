# VervoerManager Backend

.NET 9 API for the VervoerManager truck/transport management system.

**Live API**: https://api.vervoermanager.nl

## Quick Start

```bash
dotnet restore TruckManagement.sln
cd TruckManagement
dotnet run
```

Configure `ConnectionStrings__DefaultConnection` in `appsettings.Development.json` for local PostgreSQL.

## Documentation

**Start here:** [docs/INDEX.md](docs/INDEX.md) – Master map of all documentation.

| Doc | Description |
|-----|-------------|
| [CONTEXT.md](CONTEXT.md) | Entry point for AI tools & teammates |
| [docs/CONTRIBUTING_DOCS.md](docs/CONTRIBUTING_DOCS.md) | How to extend docs when adding features |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | System architecture |
| [docs/PROJECT_CONTEXT.md](docs/PROJECT_CONTEXT.md) | Domain & Dutch terms |
| [docs/BACKEND_GUIDE.md](docs/BACKEND_GUIDE.md) | .NET structure, folder layout, middleware |
| [docs/core/SERVICES.md](docs/core/SERVICES.md) | Services, interfaces, DI |
| [docs/core/MIDDLEWARE.md](docs/core/MIDDLEWARE.md) | Middleware pipeline |
| [docs/core/DATABASE.md](docs/core/DATABASE.md) | DbContext, seeding, migrations |
| [docs/api/CONTRACT.md](docs/api/CONTRACT.md) | API response envelope |
| [docs/api/ENDPOINTS.md](docs/api/ENDPOINTS.md) | All API endpoints |
| [docs/auth/FLOW.md](docs/auth/FLOW.md) | JWT, login, roles |
| [docs/data/SCHEMA.md](docs/data/SCHEMA.md) | Database entities |
| [docs/setup/DEVELOPMENT.md](docs/setup/DEVELOPMENT.md) | Local dev setup |
| [docs/setup/DEPLOYMENT.md](docs/setup/DEPLOYMENT.md) | AWS Lightsail deployment |
| [docs/setup/ENV_REFERENCE.md](docs/setup/ENV_REFERENCE.md) | Environment variables |
| [docs/features/](docs/features/) | Feature docs (DRIVERS, RIDES, PARTRIDES, etc.) |

## Deploy

Merge to `main` → auto-deploy within ~2 minutes.

**Manual deploy:**
```bash
ssh ubuntu@3.73.183.137 "sudo /usr/local/bin/deploy-backend"
```

**Logs:**
```bash
ssh ubuntu@3.73.183.137 "tail -f /var/log/auto-deploy.log"
```
