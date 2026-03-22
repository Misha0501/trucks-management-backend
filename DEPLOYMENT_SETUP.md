# Backend Deployment Setup

## Overview

The backend uses a webhook-based deployment system. When code is pushed to `main`, GitHub sends a webhook to the server, which triggers an automatic deployment.

## Setup Instructions

### 1. GitHub Webhook Configuration

Go to: **https://github.com/Misha0501/trucks-management-backend/settings/hooks/new**

Configure as follows:

- **Payload URL**: `http://3.73.183.137:8888/webhook`
- **Content type**: `application/json`
- **Secret**: `fcae34deb6f5f179040d971245e71a064f33ec758210d0215589a3ce00c0ba54`
- **Which events**: Select "Just the push event"
- **Active**: ✓ (checked)

Click "Add webhook".

### 2. Verify Setup

After adding the webhook, push a commit to `main`. You should see:

1. GitHub sends webhook to server
2. Server logs show deployment starting
3. Server pulls latest code, rebuilds Docker image, restarts container
4. Health check validates deployment

### 3. Check Deployment Logs

To view deployment logs on the server:

```bash
ssh ubuntu@3.73.183.137
sudo journalctl -u webhook-deploy.service -f
```

## System Components

### Webhook Server
- **Service**: `webhook-deploy.service`
- **Location**: `/usr/local/bin/webhook-server.py`
- **Port**: 8888
- **Status**: `sudo systemctl status webhook-deploy.service`

### Deployment Script
- **Location**: `/usr/local/bin/deploy-backend`
- **Function**: Pull code, build, deploy with zero downtime
- **Manual trigger**: `sudo /usr/local/bin/deploy-backend`

### GitHub Actions
- **Workflow**: `.github/workflows/deploy.yml`
- **Function**: Build validation only (no longer deploys directly)
- **Triggers on**: Push to `main`

## Troubleshooting

### Check if webhook is running
```bash
curl http://3.73.183.137:8888/health
# Should return: OK
```

### View recent deployments
```bash
ssh ubuntu@3.73.183.137
sudo journalctl -u webhook-deploy.service -n 100
```

### Manual deployment
```bash
ssh ubuntu@3.73.183.137
sudo /usr/local/bin/deploy-backend
```

### Restart webhook service
```bash
ssh ubuntu@3.73.183.137
sudo systemctl restart webhook-deploy.service
```

## Security

- Webhook validates HMAC-SHA256 signature
- Deploy script runs with specific sudo permissions
- Only `main` branch pushes trigger deployment
- Firewall allows port 8888 for webhook
