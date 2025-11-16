# 📋 Telegram Notifications - Production Deployment Checklist

## Pre-Deployment

- [ ] **Bot Created on Telegram**
  - Bot Username: `VervoerManager_Driver_Bot`
  - Bot Token obtained: `8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA`
  
- [ ] **Code Changes Reviewed**
  - All files committed to Git
  - Branch: `feature/telegram-notifications` (or similar)
  - Pull request reviewed and approved
  
- [ ] **Build Successful**
  - `dotnet build` runs without errors
  - No compilation warnings related to Telegram code
  - NuGet package `Telegram.Bot 19.0.0` installed

---

## Deployment Steps

### Step 1: Environment Configuration

- [ ] SSH into production server
  ```bash
  ssh root@your-server-ip
  ```

- [ ] Navigate to project directory
  ```bash
  cd /path/to/trucks-management-backend
  ```

- [ ] Add Telegram bot token to `.env` file
  ```bash
  echo "TELEGRAM_BOT_TOKEN=8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA" >> .env
  ```

- [ ] Verify `.env` file contains the token
  ```bash
  cat .env | grep TELEGRAM
  ```
  **Expected Output:**
  ```
  TELEGRAM_BOT_TOKEN=8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA
  ```

---

### Step 2: Deploy Application

- [ ] Pull latest code from Git
  ```bash
  git pull origin main  # or your deployment branch
  ```

- [ ] Stop existing containers
  ```bash
  docker-compose down
  ```

- [ ] Rebuild and start containers
  ```bash
  docker-compose up -d --build
  ```

- [ ] Verify containers are running
  ```bash
  docker ps
  ```
  **Expected:** `truckmanagement-truckmanagement-1` status is "Up"

---

### Step 3: Verify Database Migration

- [ ] Check backend logs for migration success
  ```bash
  docker logs truckmanagement-truckmanagement-1 | grep -i "migration\|telegram"
  ```
  **Expected:** No errors, migration `AddTelegramNotificationFields` applied

- [ ] Connect to PostgreSQL and verify new columns
  ```bash
  docker exec -it truckmanagement-postgresdb-1 psql -U postgres -d TruckManagement
  ```
  ```sql
  \d "Drivers"
  ```
  **Expected columns:**
  - `TelegramChatId` (bigint, nullable)
  - `TelegramNotificationsEnabled` (boolean, default false)
  - `TelegramRegisteredAt` (timestamp, nullable)
  - `TelegramRegistrationToken` (text, nullable)
  - `TelegramTokenExpiresAt` (timestamp, nullable)

- [ ] Exit PostgreSQL
  ```sql
  \q
  ```

---

### Step 4: Configure Telegram Webhook (CRITICAL)

⚠️ **This step is REQUIRED for automatic registration to work!**

- [ ] Get your production domain (must be HTTPS)
  ```
  Example: https://api.truckmanagement.com
  ```

- [ ] Set the webhook
  ```bash
  curl -X POST "https://api.telegram.org/bot8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA/setWebhook" \
    -H "Content-Type: application/json" \
    -d '{"url": "https://YOUR-ACTUAL-DOMAIN.com/telegram/webhook"}'
  ```
  **Replace `YOUR-ACTUAL-DOMAIN.com` with your real domain!**

- [ ] Verify webhook response
  **Expected:**
  ```json
  {
    "ok": true,
    "result": true,
    "description": "Webhook was set"
  }
  ```

- [ ] Check webhook status
  ```bash
  curl "https://api.telegram.org/bot8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA/getWebhookInfo"
  ```
  **Expected:**
  ```json
  {
    "ok": true,
    "result": {
      "url": "https://YOUR-ACTUAL-DOMAIN.com/telegram/webhook",
      "has_custom_certificate": false,
      "pending_update_count": 0,
      "max_connections": 40
    }
  }
  ```

---

## Post-Deployment Testing

### Test 1: Health Check

- [ ] Check backend is responding
  ```bash
  curl https://YOUR-DOMAIN.com/health  # or any public endpoint
  ```

- [ ] Check backend logs for errors
  ```bash
  docker logs truckmanagement-truckmanagement-1 --tail 100
  ```

---

### Test 2: Generate Registration Link (Admin)

- [ ] Login as Global Admin on frontend
- [ ] Navigate to any driver's details page
- [ ] Click "Genereer Activatielink" button
- [ ] Verify link is generated successfully
- [ ] Copy the registration URL

**Alternative (API test):**
```bash
curl -X GET "https://YOUR-DOMAIN.com/drivers/{driverId}/telegram/registration-link" \
  -H "Authorization: Bearer {adminToken}"
```

---

### Test 3: Driver Registration (End-to-End)

- [ ] Open registration URL from Test 2 on a test phone/Telegram app
- [ ] Click "START" in the bot chat
- [ ] Check database for driver's Telegram data
  ```sql
  SELECT "Id", "TelegramChatId", "TelegramNotificationsEnabled" 
  FROM "Drivers" 
  WHERE "Id" = '{testDriverId}';
  ```
  **Expected:**
  - `TelegramChatId`: Non-null (e.g., 123456789)
  - `TelegramNotificationsEnabled`: `true`

---

### Test 4: Send Test Notification

- [ ] Use test message endpoint (global admin only)
  ```bash
  curl -X POST "https://YOUR-DOMAIN.com/telegram/test" \
    -H "Authorization: Bearer {globalAdminToken}" \
    -H "Content-Type: application/json" \
    -d '{"driverId": "{testDriverId}", "message": "🧪 Test bericht!"}'
  ```

- [ ] Verify driver receives message on Telegram
- [ ] Message should appear within 1-2 seconds

---

### Test 5: Ride Notification (Critical Path)

- [ ] Create a ride for TODAY via frontend/API
- [ ] Assign a driver who has Telegram enabled
- [ ] Verify driver receives "Nieuwe rit toegewezen!" notification
- [ ] Update the ride details
- [ ] Verify driver receives "Rit gewijzigd!" notification
- [ ] Delete the ride
- [ ] Verify driver receives "Rit geannuleerd!" notification

---

## Rollback Plan (If Issues Occur)

### Emergency Rollback

- [ ] Stop containers
  ```bash
  docker-compose down
  ```

- [ ] Checkout previous working commit
  ```bash
  git checkout <previous-commit-hash>
  ```

- [ ] Rebuild and restart
  ```bash
  docker-compose up -d --build
  ```

### Database Rollback (If Migration Fails)

⚠️ **Only if absolutely necessary!**

- [ ] Remove migration from database
  ```bash
  docker exec -it truckmanagement-truckmanagement-1 dotnet ef database update <previous-migration-name>
  ```

- [ ] Delete migration file
  ```bash
  rm TruckManagement/Migrations/*_AddTelegramNotificationFields.cs
  ```

---

## Monitoring (First 24 Hours)

### Key Metrics to Watch

- [ ] **Backend Logs** - Check every 2-3 hours
  ```bash
  docker logs -f truckmanagement-truckmanagement-1 | grep -i "telegram\|error"
  ```
  **Watch for:** `[Telegram]` prefixed messages, any exceptions

- [ ] **Webhook Status** - Check 2x per day
  ```bash
  curl "https://api.telegram.org/bot8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA/getWebhookInfo"
  ```
  **Watch for:** `pending_update_count` should stay near 0

- [ ] **Driver Registrations** - Check database
  ```sql
  SELECT COUNT(*) FROM "Drivers" WHERE "TelegramNotificationsEnabled" = true;
  ```

- [ ] **Failed Notifications** - Check logs for failures
  ```bash
  docker logs truckmanagement-truckmanagement-1 | grep "\[Telegram\] Failed"
  ```

---

## Common Issues & Solutions

### Issue: Webhook returns 404
**Symptoms:** Drivers click link but nothing happens  
**Solution:**
1. Check webhook URL is correct: `curl https://api.telegram.org/bot{token}/getWebhookInfo`
2. Verify endpoint is accessible: `curl https://YOUR-DOMAIN.com/telegram/webhook -X POST`
3. Check nginx/reverse proxy configuration

### Issue: "Connection refused" on port 5460
**Symptoms:** Backend can't connect to PostgreSQL  
**Solution:**
1. Check PostgreSQL container is running: `docker ps`
2. Check port mapping in `compose.yaml`
3. Restart PostgreSQL: `docker-compose restart postgresdb`

### Issue: Notifications not being sent
**Symptoms:** Rides are created but driver doesn't receive notification  
**Solution:**
1. Verify ride's `PlannedDate` is TODAY (not future date)
2. Check driver has `TelegramNotificationsEnabled = true`
3. Check driver has `TelegramChatId` set
4. Check backend logs for exceptions

### Issue: Token expired error
**Symptoms:** Driver clicks link, registration fails  
**Solution:**
- Registration links expire after 24 hours
- Generate a new link for the driver
- Check `TelegramTokenExpiresAt` in database

---

## Success Criteria

✅ Deployment is successful if ALL of these are true:

- [ ] Backend builds and runs without errors
- [ ] Database migration applied successfully
- [ ] Telegram webhook is set and responding
- [ ] At least 1 driver successfully registered
- [ ] Test notification received by registered driver
- [ ] Ride assignment notification works
- [ ] No critical errors in backend logs

---

## Rollback Triggers

❌ Rollback immediately if ANY of these occur:

- [ ] Backend crashes on startup
- [ ] Database migration fails and can't be fixed quickly
- [ ] More than 30% of rides fail to trigger notifications
- [ ] Webhook continuously returns errors (500/502/503)
- [ ] Production system becomes unstable

---

## Sign-Off

### Deployment Team
- **Deployed By:** _______________________
- **Date:** _______________________
- **Time:** _______________________

### Post-Deployment Testing
- **Tested By:** _______________________
- **All Tests Passed:** ☐ Yes  ☐ No
- **Notes:** _______________________

### Approval
- **Approved By:** _______________________
- **Signature:** _______________________

---

**Deployment Status: ☐ In Progress  ☐ Complete  ☐ Rolled Back**

