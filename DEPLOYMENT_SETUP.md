# Backend Deployment Setup

## Overview

The backend uses an **automatic polling deployment system**. A cron job on the server checks for new commits every 2 minutes and automatically deploys them.

## How It Works

1. **Push to `main`** → Triggers GitHub Actions build validation
2. **Server cron job** → Checks for new commits every 2 minutes
3. **Auto-deploy** → Pulls code, rebuilds Docker, zero-downtime restart
4. **Health check** → Validates deployment success

## Deployment Timeline

- **Maximum delay**: 2 minutes after push to `main`
- **Typical delay**: 30-120 seconds
- **Build time**: ~60 seconds (Docker + .NET)

## Check Deployment Status

### View deployment logs:
```bash
ssh ubuntu@3.73.183.137
tail -f /var/log/auto-deploy.log
```

### Check if deployment is active:
```bash
ssh ubuntu@3.73.183.137
ps aux | grep deploy-backend
```

### View recent deployments:
```bash
ssh ubuntu@3.73.183.137
cat /var/log/auto-deploy.log | tail -50
```

## System Components

### Auto-Deploy Checker
- **Cron schedule**: Every 2 minutes (`*/2 * * * *`)
- **Script**: `/usr/local/bin/auto-deploy-checker`
- **Log file**: `/var/log/auto-deploy.log`

### Deployment Script
- **Location**: `/usr/local/bin/deploy-backend`
- **Function**: Pull code, build, deploy with zero downtime
- **Manual trigger**: `sudo /usr/local/bin/deploy-backend`

### GitHub Actions
- **Workflow**: `.github/workflows/deploy.yml`
- **Function**: Build validation only
- **Triggers on**: Push to `main`

## Troubleshooting

### Force immediate deployment
```bash
ssh ubuntu@3.73.183.137
sudo /usr/local/bin/deploy-backend
```

### Check cron job
```bash
ssh ubuntu@3.73.183.137
crontab -l | grep auto-deploy
```

### View live deployment
```bash
ssh ubuntu@3.73.183.137
tail -f /var/log/auto-deploy.log
```

### Check container status
```bash
ssh ubuntu@3.73.183.137
docker ps | grep truck
docker logs backend-truckmanagement-1 --tail 50
```

## Security

- Deploy script runs with specific sudo permissions
- Only `main` branch is monitored
- Git operations use SSH key authentication
- No external network dependencies
