# VervoerManager – Deployment Guide (AWS Lightsail)

## Live Environment

| Service | URL | Server Path |
|---------|-----|-------------|
| Frontend | https://vervoermanager.nl | `/var/www/frontend` |
| API | https://api.vervoermanager.nl | `/var/www/backend` |

**Server**: AWS Lightsail `3.73.183.137` (Ubuntu)

## Architecture

```
Internet → vervoermanager.nl / api.vervoermanager.nl
              → Nginx reverse proxy
              → Frontend: PM2 (port 3000)
              → Backend: Docker (port 9090 internal)
```

## Backend Deployment

- **Repo**: https://github.com/Misha0501/trucks-management-backend
- **Deploy script**: `deploy-webhook.sh` (installed as `/usr/local/bin/deploy-backend`)
- **Auto-deploy**: Cron every 2 min (`auto-deploy-checker.sh`)
- **Log**: `/var/log/auto-deploy.log`

**Deploy steps** (in script):
1. `git pull origin main`
2. `docker compose build truckmanagement`
3. `docker compose up -d --no-deps truckmanagement` (rolling update)
4. Health check on `http://localhost:9090`

## Frontend Deployment

- **Repo**: https://github.com/andreyroizin/trucks-management-frontend
- **Deploy script**: `deploy-frontend.sh` (installed as `/usr/local/bin/deploy-frontend`)
- **Auto-deploy**: Cron every 2 min (`auto-deploy-checker.sh`)
- **Log**: `/var/log/auto-deploy-frontend.log`

**Deploy steps** (in script):
1. `git pull origin main`
2. `npm ci --production=false`
3. `NEXT_PUBLIC_API_BASE_URL="https://api.vervoermanager.nl" npm run build`
4. `pm2 reload trucks-frontend` (zero downtime)

## Manual Deploy

```bash
# Backend
ssh ubuntu@3.73.183.137 "sudo /usr/local/bin/deploy-backend"

# Frontend
ssh ubuntu@3.73.183.137 "sudo /usr/local/bin/deploy-frontend"
```

## Useful Commands

```bash
# View deploy logs
ssh ubuntu@3.73.183.137 "tail -f /var/log/auto-deploy.log"
ssh ubuntu@3.73.183.137 "tail -f /var/log/auto-deploy-frontend.log"

# Docker (backend)
ssh ubuntu@3.73.183.137 "docker ps | grep truck"
ssh ubuntu@3.73.183.137 "docker logs backend-truckmanagement-1 --tail 50"

# PM2 (frontend)
ssh ubuntu@3.73.183.137 "pm2 list"
ssh ubuntu@3.73.183.137 "pm2 logs trucks-frontend --lines 30"
```

## Environment (Production)

- **Backend**: `.env` in `/var/www/backend` – DB, SMTP, Telegram
- **Frontend**: Build-time `NEXT_PUBLIC_API_BASE_URL` only
