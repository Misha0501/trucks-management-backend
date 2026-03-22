# Truck Management Backend

.NET Core backend API for the truck management system.

## Quick Deploy

When you merge to `main`, the server automatically deploys within 2 minutes.

To deploy immediately:

```bash
ssh ubuntu@3.73.183.137 "sudo /usr/local/bin/deploy-backend"
```

## Local Development

```bash
# Restore dependencies
dotnet restore TruckManagement.sln

# Build
dotnet build TruckManagement.sln

# Run locally
cd TruckManagement
dotnet run
```

## Deployment

The server runs a cron job that checks for new commits every 2 minutes and automatically deploys them with zero downtime.

**Manual deployment:**
```bash
ssh ubuntu@3.73.183.137 "sudo /usr/local/bin/deploy-backend"
```

**View deployment logs:**
```bash
ssh ubuntu@3.73.183.137 "tail -f /var/log/auto-deploy.log"
```

**Check API status:**
```bash
curl https://api.vervoermanager.nl
```
