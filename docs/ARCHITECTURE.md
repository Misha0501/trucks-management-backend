# VervoerManager – System Architecture

## High-Level Overview

```
┌─────────────────────┐     HTTPS      ┌─────────────────────┐
│   Users (Browser)   │ ◄────────────►│  AWS Lightsail      │
│   vervoermanager.nl │                │  3.73.183.137       │
└─────────────────────┘                │                     │
                                       │  Nginx (reverse     │
                                       │   proxy)             │
                                       │                     │
                                       │  ┌───────────────┐  │
                                       │  │ Frontend      │  │
                                       │  │ Next.js + PM2 │  │
                                       │  │ Port 3000     │  │
                                       │  └───────┬───────┘  │
                                       │          │         │
                                       │  ┌───────▼───────┐  │
                                       │  │ Backend API   │  │
                                       │  │ .NET 9 Docker │  │
                                       │  │ Port 9090     │  │
                                       │  └───────┬───────┘  │
                                       │          │         │
                                       │  ┌───────▼───────┐  │
                                       │  │ PostgreSQL    │  │
                                       │  │ Port 5460     │  │
                                       │  └───────────────┘  │
                                       └─────────────────────┘
```

## This Repo (Backend)
- **Stack**: .NET 9, ASP.NET Core, PostgreSQL, Entity Framework Core
- **Docker**: `truckmanagement` (app), `postgresdb`, `pgadmin`
- **Ports**: 9090 (HTTP), 9091 (HTTPS - if used)

## Related Repositories
| Repo | Purpose |
|------|---------|
| trucks-management-frontend | Next.js web app |
| trucks-management-backend | This repo – API + DB |

## Live API
- **URL**: https://api.vervoermanager.nl
- **Server**: `ubuntu@3.73.183.137` (AWS Lightsail)
- **Deploy path**: `/var/www/backend`
- **Storage**: `/var/www/storage` (signed contracts, uploads)
