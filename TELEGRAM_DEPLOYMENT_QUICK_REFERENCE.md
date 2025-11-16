# 📋 Telegram Deployment - Quick Reference Card

## 🎯 Essential Info

- **Bot Token**: `8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA`
- **Bot Username**: `VervoerManager_Driver_Bot`
- **Production Server**: `root@h2871417.stratoserver.net`
- **Git Repo**: `https://github.com/Misha0501/trucks-management-backend.git`

---

## 🚀 Deployment Commands (Copy-Paste Ready)

### Step 1: SSH to Server
```bash
ssh root@h2871417.stratoserver.net
```

### Step 2: Navigate to Project & Add Token
```bash
# Find project directory
cd /root/trucks-management-backend || cd /var/www/trucks-management-backend || cd ~/trucks-management-backend

# Verify you're in the right place
ls -la compose.yaml

# Add Telegram token to .env
echo "TELEGRAM_BOT_TOKEN=8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA" >> .env

# Verify
cat .env | grep TELEGRAM
```

### Step 3: Deploy
```bash
# Ensure you're on main branch
git checkout main

# Pull latest code
git pull origin main

# Rebuild and restart
docker compose down
docker compose up -d --build
```

### Step 4: Verify Deployment
```bash
# Check containers are running
docker ps

# Check logs (no errors expected)
docker logs $(docker ps --filter "ancestor=truckmanagement" --format "{{.Names}}") --tail 50
```

---

## 🔗 Set Telegram Webhook

### Find Your Domain First
```bash
# Test backend accessibility
curl http://localhost:9090/health

# Check nginx config (if applicable)
cat /etc/nginx/sites-enabled/default | grep -A 10 "location"
```

### Set Webhook (Replace YOUR-DOMAIN.com!)
```bash
curl -X POST "https://api.telegram.org/bot8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA/setWebhook" \
  -H "Content-Type: application/json" \
  -d '{"url": "https://YOUR-DOMAIN.com/telegram/webhook"}'
```

### Verify Webhook
```bash
curl "https://api.telegram.org/bot8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA/getWebhookInfo"
```

Expected: `"ok": true`, `"url": "https://YOUR-DOMAIN.com/telegram/webhook"`

---

## ✅ Quick Tests

### Test 1: Generate Registration Link (API)
```bash
# Replace YOUR-DOMAIN and ADMIN-TOKEN
curl -X GET "https://YOUR-DOMAIN.com/drivers/17aeead4-3d83-46ec-9cc1-e5412c5d0317/telegram/registration-link" \
  -H "Authorization: Bearer YOUR-ADMIN-TOKEN"
```

### Test 2: Check Database Registration
```bash
docker exec -it $(docker ps --filter "ancestor=postgres:15-alpine" --format "{{.Names}}") \
  psql -U postgres -d TruckManagement \
  -c "SELECT \"Id\", \"TelegramChatId\", \"TelegramNotificationsEnabled\" FROM \"Drivers\" WHERE \"TelegramNotificationsEnabled\" = true;"
```

### Test 3: Send Test Message
```bash
# Replace YOUR-DOMAIN, GLOBAL-ADMIN-TOKEN, and DRIVER-ID
curl -X POST "https://YOUR-DOMAIN.com/telegram/test" \
  -H "Authorization: Bearer YOUR-GLOBAL-ADMIN-TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"driverId": "DRIVER-ID", "message": "🧪 Test from production!"}'
```

---

## 🐛 Troubleshooting Commands

### Check Logs for Errors
```bash
docker logs $(docker ps --filter "ancestor=truckmanagement" --format "{{.Names}}") | grep -i "error\|exception\|fail"
```

### Check Telegram-Related Logs
```bash
docker logs $(docker ps --filter "ancestor=truckmanagement" --format "{{.Names}}") | grep -i "telegram"
```

### Check Webhook Status
```bash
curl "https://api.telegram.org/bot8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA/getWebhookInfo"
```

### Check .env File
```bash
cat .env | grep TELEGRAM
```

### Restart Backend
```bash
docker compose restart truckmanagement
```

### Full Rebuild
```bash
docker compose down
docker compose up -d --build
```

---

## 📊 Monitoring Commands

### Watch Logs in Real-Time
```bash
docker logs -f $(docker ps --filter "ancestor=truckmanagement" --format "{{.Names}}") | grep -i "telegram\|error"
```

### Count Registered Drivers
```bash
docker exec -it $(docker ps --filter "ancestor=postgres:15-alpine" --format "{{.Names}}") \
  psql -U postgres -d TruckManagement \
  -c "SELECT COUNT(*) FROM \"Drivers\" WHERE \"TelegramNotificationsEnabled\" = true;"
```

### Check Notification Failures
```bash
docker logs $(docker ps --filter "ancestor=truckmanagement" --format "{{.Names}}") | grep "\[Telegram\] Notification failed"
```

---

## 🔄 Rollback (If Issues)

```bash
# Stop containers
docker compose down

# Checkout previous working commit
git log --oneline -10  # Find previous commit
git checkout <commit-hash>

# Rebuild
docker compose up -d --build
```

---

## ✅ Deployment Checklist

- [ ] SSH'd to production server
- [ ] Navigated to project directory
- [ ] Added `TELEGRAM_BOT_TOKEN` to `.env`
- [ ] Verified token in `.env`
- [ ] Pulled latest code (`git pull origin main`)
- [ ] Rebuilt containers (`docker compose up -d --build`)
- [ ] Verified containers running (`docker ps`)
- [ ] Checked logs for errors
- [ ] Set Telegram webhook
- [ ] Verified webhook status
- [ ] Tested registration link generation
- [ ] Registered 1 test driver
- [ ] Sent test notification
- [ ] Created test ride for today
- [ ] Driver received ride notification

---

## 🆘 Common Issues

### Issue: "TELEGRAM_BOT_TOKEN not set"
**Fix:**
```bash
echo "TELEGRAM_BOT_TOKEN=8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA" >> .env
docker compose restart
```

### Issue: Webhook returns 404
**Fix:**
1. Check webhook URL is correct
2. Test endpoint: `curl -X POST https://YOUR-DOMAIN.com/telegram/webhook -d '{"test":true}'`
3. Check nginx/reverse proxy configuration

### Issue: Driver doesn't receive notifications
**Fix:**
1. Verify ride's `PlannedDate` is TODAY
2. Check driver has `TelegramNotificationsEnabled = true`
3. Check driver has `TelegramChatId` set
4. Check logs for errors

### Issue: Registration link expired
**Fix:** Links expire after 24 hours. Generate a new link.

---

## 📞 Need Help?

1. Check full guide: `PRODUCTION_DEPLOYMENT_TELEGRAM.md`
2. Review logs: `docker logs CONTAINER_NAME --tail 100`
3. Check webhook: `curl https://api.telegram.org/bot.../getWebhookInfo`
4. Verify database: `docker exec -it POSTGRES_CONTAINER psql ...`

---

**Save this file for quick reference during deployment! 🚀**

