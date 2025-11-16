# 🚀 Production Deployment - Telegram Notifications

## 📋 Current Deployment Setup

Your project uses:
- **Git Repository**: `https://github.com/Misha0501/trucks-management-backend.git`
- **Deployment Method**: Direct `git pull` + `docker compose` on production server
- **Current Branch**: `main`
- **Docker Compose**: Uses `.env` file for environment variables (overrides `appsettings.json`)
- **Production Server**: `root@h2871417.stratoserver.net`

---

## ⚙️ Required Environment Variables

Add this **ONE** variable to your production `.env` file:

```bash
TELEGRAM_BOT_TOKEN=8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA
```

That's it! The `Telegram__BotUsername` is already hardcoded in `compose.yaml` (line 21).

---

## 📝 Step-by-Step Deployment

### 1️⃣ SSH into Production Server

```bash
ssh root@h2871417.stratoserver.net
```

### 2️⃣ Navigate to Project Directory

```bash
# Find your project directory (likely one of these):
cd /root/trucks-management-backend
# OR
cd /var/www/trucks-management-backend
# OR
cd ~/trucks-management-backend

# Verify you're in the right place:
ls -la compose.yaml
```

### 3️⃣ Add Telegram Token to .env File

**Check if .env exists:**
```bash
ls -la .env
```

**If .env exists, add the token:**
```bash
echo "TELEGRAM_BOT_TOKEN=8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA" >> .env
```

**If .env doesn't exist, create it with all required variables:**
```bash
cat > .env << 'EOF'
# Database
CONNECTION_STRING=Host=postgresdb;Port=5432;Database=TruckManagement;Username=postgres;Password=YOUR_POSTGRES_PASSWORD

# PostgreSQL
POSTGRES_USER=postgres
POSTGRES_PASSWORD=YOUR_POSTGRES_PASSWORD
POSTGRES_DB=TruckManagement
POSTGRES_PORT=5460

# SMTP (Email)
SMTP_HOST=YOUR_SMTP_HOST
SMTP_PORT=587
SMTP_USERNAME=YOUR_SMTP_USERNAME
SMTP_PASSWORD=YOUR_SMTP_PASSWORD
SMTP_FROM_ADDRESS=noreply@yourdomain.com

# Frontend
FRONTEND_RESET_PASSWORD_URL=https://your-frontend-url.com/reset-password

# pgAdmin
PGADMIN_DEFAULT_EMAIL=admin@example.com
PGADMIN_DEFAULT_PASSWORD=YOUR_PGADMIN_PASSWORD

# Telegram (NEW!)
TELEGRAM_BOT_TOKEN=8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA
EOF
```

**Verify the token is set:**
```bash
cat .env | grep TELEGRAM
```
Expected output:
```
TELEGRAM_BOT_TOKEN=8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA
```

### 4️⃣ Check Current Branch and Pull Latest Code

```bash
# Check which branch you're on
git branch

# Make sure you're on main
git checkout main

# Pull latest code
git pull origin main

# Verify the Telegram code is present
ls -la TruckManagement/Services/TelegramNotificationService.cs
```

### 5️⃣ Rebuild and Deploy

```bash
docker compose down
docker compose up -d --build
```

### 6️⃣ Verify Containers Are Running

```bash
docker ps
```

Check that the backend container status shows "Up". The container name might be:
- `trucks-management-backend-truckmanagement-1` (if folder is `trucks-management-backend`)
- `truckmanagement-truckmanagement-1` (if folder is `truckmanagement`)

**Note the exact container name for the next steps!**

### 7️⃣ Check Backend Logs

```bash
# Replace CONTAINER_NAME with the actual name from step 6
docker logs CONTAINER_NAME --tail 50

# Or find it automatically:
docker logs $(docker ps --filter "ancestor=truckmanagement" --format "{{.Names}}") --tail 50
```

Look for:
- ✅ No errors during startup
- ✅ Database migration applied successfully
- ✅ `Now listening on: http://[::]:8080`

---

## 🔗 Configure Telegram Webhook (CRITICAL!)

⚠️ **This step is REQUIRED for driver registration to work!**

### Find Your Production Domain

Your backend must be accessible via **HTTPS**.

**First, check how your backend is exposed:**

```bash
# Check nginx configuration (if using nginx reverse proxy)
cat /etc/nginx/sites-enabled/default | grep -A 10 "location"

# Or check what port the backend is running on
docker ps | grep truckmanagement
```

Your backend is running on port `9090` inside Docker. You need to find:
1. **Is there a reverse proxy (nginx)?** 
   - If YES: What's the domain and path? (e.g., `https://yourdomain.com/api`)
   - If NO: Is port 9090 exposed directly? (e.g., `https://yourdomain.com:9090`)

Common setups:
- Plesk with proxy: `https://your-domain.com` or `https://your-domain.com/api`
- Direct Docker port: `https://your-domain.com:9090`
- Subdomain: `https://api.your-domain.com`

### Quick Test: Find Your Backend URL

Run these commands on the production server to test accessibility:

```bash
# Test if backend responds on localhost
curl http://localhost:9090/health 2>/dev/null || curl http://localhost:9090/api/health 2>/dev/null || echo "No health endpoint found"

# Check if SSL/HTTPS is configured
curl https://$(hostname -f):9090/ 2>/dev/null || echo "No HTTPS on port 9090"

# If you have a domain, test it
curl https://YOUR-DOMAIN.com/health
```

**Once you confirm your backend URL works, proceed to set the webhook.**

### Set the Webhook

Replace `YOUR-DOMAIN.com` with your **actual production domain**:

```bash
curl -X POST "https://api.telegram.org/bot8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA/setWebhook" \
  -H "Content-Type: application/json" \
  -d '{"url": "https://YOUR-DOMAIN.com/telegram/webhook"}'
```

**Example (if your API is at `https://api.truckmanagement.com`):**
```bash
curl -X POST "https://api.telegram.org/bot8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA/setWebhook" \
  -H "Content-Type: application/json" \
  -d '{"url": "https://api.truckmanagement.com/telegram/webhook"}'
```

**Expected Response:**
```json
{
  "ok": true,
  "result": true,
  "description": "Webhook was set"
}
```

### Verify Webhook

```bash
curl "https://api.telegram.org/bot8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA/getWebhookInfo"
```

**Expected Response:**
```json
{
  "ok": true,
  "result": {
    "url": "https://YOUR-DOMAIN.com/telegram/webhook",
    "has_custom_certificate": false,
    "pending_update_count": 0
  }
}
```

---

## ✅ Testing in Production

### Test 1: Generate Registration Link

**Via API (using your customer admin token):**
```bash
curl -X GET "https://YOUR-DOMAIN.com/drivers/17aeead4-3d83-46ec-9cc1-e5412c5d0317/telegram/registration-link" \
  -H "Authorization: Bearer YOUR_CUSTOMER_ADMIN_TOKEN"
```

**Expected Response:**
```json
{
  "isSuccess": true,
  "data": {
    "registrationUrl": "https://t.me/VervoerManager_Driver_Bot?start=abc123...",
    "expiresAt": "2025-11-17T12:00:00Z"
  }
}
```

### Test 2: Register a Driver

1. Copy the `registrationUrl` from Test 1
2. Open it on your phone (or send to a test driver)
3. Click **START** in the Telegram bot
4. You should receive: "✅ **Registratie geslaagd!** Je ontvangt nu meldingen..."

### Test 3: Verify Registration in Database

```bash
# Replace CONTAINER_NAME with your actual postgres container name
# (likely: trucks-management-backend-postgresdb-1 or similar)
docker exec -it POSTGRES_CONTAINER_NAME psql -U postgres -d TruckManagement

# Or find it automatically:
docker exec -it $(docker ps --filter "ancestor=postgres:15-alpine" --format "{{.Names}}") psql -U postgres -d TruckManagement
```

```sql
SELECT "Id", "TelegramChatId", "TelegramNotificationsEnabled", "TelegramRegisteredAt"
FROM "Drivers" 
WHERE "Id" = '17aeead4-3d83-46ec-9cc1-e5412c5d0317';
```

**Expected:**
- `TelegramChatId`: A number (e.g., 999888777)
- `TelegramNotificationsEnabled`: `true`
- `TelegramRegisteredAt`: Current timestamp

Exit PostgreSQL:
```sql
\q
```

### Test 4: Send Test Notification

```bash
curl -X POST "https://YOUR-DOMAIN.com/telegram/test" \
  -H "Authorization: Bearer YOUR_GLOBAL_ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"driverId": "17aeead4-3d83-46ec-9cc1-e5412c5d0317", "message": "🧪 Test bericht vanuit productie!"}'
```

The driver should receive the test message on Telegram within 1-2 seconds.

### Test 5: Real Notification (Critical!)

1. **Create a ride for TODAY** via the frontend
2. **Assign the registered driver**
3. Driver should receive: "🚚 **Nieuwe rit toegewezen!**"
4. **Update the ride** (change time, address, etc.)
5. Driver should receive: "⚠️ **Rit gewijzigd!**"
6. **Delete the ride**
7. Driver should receive: "❌ **Rit geannuleerd!**"

---

## 🐛 Troubleshooting

### Issue: "Connection refused" to PostgreSQL

**Check if PostgreSQL is running:**
```bash
docker ps | grep postgres
```

**Restart PostgreSQL:**
```bash
docker compose restart postgresdb
```

### Issue: Webhook returns 404

**Check webhook status:**
```bash
curl "https://api.telegram.org/bot8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA/getWebhookInfo"
```

**Check if backend is accessible:**
```bash
curl -X POST https://YOUR-DOMAIN.com/telegram/webhook \
  -H "Content-Type: application/json" \
  -d '{"test": true}'
```

If you get 404, check your nginx/reverse proxy configuration.

### Issue: Notifications not being sent

**Check backend logs:**
```bash
docker logs trucks-management-backend-truckmanagement-1 | grep -i telegram
```

**Common causes:**
1. Ride's `PlannedDate` is not TODAY
2. Driver doesn't have `TelegramNotificationsEnabled = true`
3. Driver doesn't have `TelegramChatId` set
4. Telegram API timeout (check for timeout errors in logs)

### Issue: "TELEGRAM_BOT_TOKEN not set" warning

**Verify .env file:**
```bash
cat .env | grep TELEGRAM
```

**Restart containers:**
```bash
docker compose restart
```

---

## 📊 Monitoring (First 24 Hours)

### Check Backend Logs Regularly

```bash
# Watch logs in real-time (replace CONTAINER_NAME)
docker logs -f CONTAINER_NAME | grep -i "telegram\|error"

# Or use automatic container detection:
docker logs -f $(docker ps --filter "ancestor=truckmanagement" --format "{{.Names}}") | grep -i "telegram\|error"

# Check for notification failures
docker logs $(docker ps --filter "ancestor=truckmanagement" --format "{{.Names}}") | grep "\[Telegram\] Notification failed"
```

### Check Webhook Status

```bash
curl "https://api.telegram.org/bot8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA/getWebhookInfo"
```

Watch for `pending_update_count` - it should stay near 0.

### Check Driver Registrations

```bash
# Quick count of registered drivers
docker exec -it $(docker ps --filter "ancestor=postgres:15-alpine" --format "{{.Names}}") \
  psql -U postgres -d TruckManagement \
  -c "SELECT COUNT(*) FROM \"Drivers\" WHERE \"TelegramNotificationsEnabled\" = true;"
```

---

## 🔄 Rollback Plan

If something goes wrong:

```bash
# Stop containers
docker compose down

# Checkout previous working commit
git log --oneline -10  # Find the previous commit hash
git checkout <previous-commit-hash>

# Rebuild and restart
docker compose up -d --build
```

---

## ✅ Success Checklist

Deployment is successful when:

- [ ] Backend builds and runs without errors
- [ ] `.env` file contains `TELEGRAM_BOT_TOKEN`
- [ ] Telegram webhook is set correctly
- [ ] At least 1 driver successfully registered
- [ ] Test notification received by driver
- [ ] Ride assignment notification works
- [ ] No critical errors in logs

---

## 📞 Support

If you encounter issues:

1. Check logs: 
   ```bash
   docker logs $(docker ps --filter "ancestor=truckmanagement" --format "{{.Names}}") --tail 100
   ```

2. Check webhook: 
   ```bash
   curl https://api.telegram.org/bot8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA/getWebhookInfo
   ```

3. Check database connection: 
   ```bash
   docker exec -it $(docker ps --filter "ancestor=postgres:15-alpine" --format "{{.Names}}") psql -U postgres -l
   ```

4. Review this guide and try the troubleshooting steps

---

**Good luck with your deployment! 🚀**

