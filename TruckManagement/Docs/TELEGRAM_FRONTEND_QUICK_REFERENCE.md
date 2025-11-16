# 📱 Telegram Notifications - Frontend Quick Reference

## 🎯 Overview for Frontend Team

The backend now supports **automated Telegram notifications** for drivers. This document provides everything you need to integrate this feature into the frontend.

---

## 🔌 API Endpoints

### 1. Generate Telegram Registration Link

**Endpoint:** `GET /drivers/{driverId}/telegram/registration-link`  
**Auth:** `globalAdmin`, `customerAdmin`  
**Purpose:** Admin generates a unique link for a driver to register with the Telegram bot

#### Request
```http
GET /drivers/a1b2c3d4-e5f6-7890-abcd-ef1234567890/telegram/registration-link
Authorization: Bearer {adminToken}
```

#### Response (Success)
```json
{
  "success": true,
  "data": {
    "driverId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "driverName": "Jan de Vries",
    "registrationUrl": "https://t.me/VervoerManager_Driver_Bot?start=abc123def456...",
    "expiresAt": "2025-11-17T12:00:00Z",
    "instructions": "De chauffeur moet op deze link klikken om Telegram notificaties te activeren.",
    "alreadyRegistered": false
  }
}
```

#### Response (Already Registered)
```json
{
  "success": true,
  "data": {
    "driverId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "driverName": "Jan de Vries",
    "registrationUrl": "https://t.me/VervoerManager_Driver_Bot?start=xyz789...",
    "expiresAt": "2025-11-17T12:00:00Z",
    "instructions": "De chauffeur moet op deze link klikken om Telegram notificaties te activeren.",
    "alreadyRegistered": true  // ⚠️ Driver is already registered but can re-register
  }
}
```

---

### 2. Disable Telegram Notifications

**Endpoint:** `DELETE /drivers/{driverId}/telegram`  
**Auth:** `globalAdmin`, `customerAdmin`  
**Purpose:** Admin disables Telegram notifications for a driver

#### Request
```http
DELETE /drivers/a1b2c3d4-e5f6-7890-abcd-ef1234567890/telegram
Authorization: Bearer {adminToken}
```

#### Response
```json
{
  "success": true,
  "message": "Telegram notifications disabled successfully."
}
```

---

### 3. Send Test Message (Testing Only)

**Endpoint:** `POST /telegram/test`  
**Auth:** `globalAdmin` only  
**Purpose:** Admin sends a test message to verify Telegram is working

#### Request
```http
POST /telegram/test
Authorization: Bearer {globalAdminToken}
Content-Type: application/json

{
  "driverId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "message": "🧪 Test bericht van VervoerManager"
}
```

#### Response
```json
{
  "success": true,
  "data": {
    "message": "Test message sent successfully.",
    "sentTo": 123456789,
    "messageContent": "🧪 Test bericht van VervoerManager"
  }
}
```

---

## 🎨 Frontend UI Recommendations

### Driver Details Page - Telegram Section

```jsx
// Suggested UI Component
import { useState } from 'react';
import { QRCodeSVG } from 'qrcode.react';

function DriverTelegramSection({ driverId, driver }) {
  const [registrationLink, setRegistrationLink] = useState(null);
  const [loading, setLoading] = useState(false);

  const generateLink = async () => {
    setLoading(true);
    try {
      const response = await fetch(
        `/drivers/${driverId}/telegram/registration-link`,
        {
          headers: { Authorization: `Bearer ${token}` }
        }
      );
      const data = await response.json();
      setRegistrationLink(data.data);
    } catch (error) {
      console.error('Failed to generate link:', error);
    }
    setLoading(false);
  };

  const disableNotifications = async () => {
    if (!confirm('Weet u zeker dat u Telegram notificaties wilt uitschakelen?')) {
      return;
    }
    await fetch(`/drivers/${driverId}/telegram`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` }
    });
    // Refresh driver data
  };

  return (
    <div className="telegram-section">
      <h3>📱 Telegram Notificaties</h3>
      
      {/* Status Badge */}
      {driver.telegramNotificationsEnabled ? (
        <div className="status-badge active">
          ✅ Actief - Geregistreerd op {formatDate(driver.telegramRegisteredAt)}
        </div>
      ) : (
        <div className="status-badge inactive">
          ⚠️ Niet geactiveerd
        </div>
      )}

      {/* Actions */}
      {!driver.telegramNotificationsEnabled && (
        <button onClick={generateLink} disabled={loading}>
          {loading ? 'Bezig...' : '🔗 Genereer Activatielink'}
        </button>
      )}

      {driver.telegramNotificationsEnabled && (
        <button onClick={disableNotifications} className="btn-danger">
          🚫 Uitschakelen
        </button>
      )}

      {/* Registration Link & QR Code */}
      {registrationLink && (
        <div className="registration-card">
          <h4>Activatielink gegenereerd</h4>
          <p className="instructions">{registrationLink.instructions}</p>
          
          {/* Copy Link Button */}
          <div className="link-container">
            <input 
              type="text" 
              value={registrationLink.registrationUrl} 
              readOnly 
            />
            <button onClick={() => navigator.clipboard.writeText(registrationLink.registrationUrl)}>
              📋 Kopiëren
            </button>
          </div>

          {/* QR Code */}
          <div className="qr-code-container">
            <QRCodeSVG 
              value={registrationLink.registrationUrl} 
              size={200}
              level="M"
            />
            <p className="qr-instructions">
              Scan deze QR-code met je telefoon of klik op de link
            </p>
          </div>

          {/* Expiry Warning */}
          <p className="expiry-warning">
            ⏳ Link verloopt op: {formatDateTime(registrationLink.expiresAt)}
          </p>
        </div>
      )}
    </div>
  );
}
```

---

## 🎯 User Flow (Driver Perspective)

1. **Admin clicks "Genereer Activatielink"** in driver details page
2. **Frontend displays:**
   - Registration URL (copyable)
   - QR code (scannable)
   - Instructions in Dutch
   - Expiry time (24 hours)
3. **Admin shares link with driver** (via email, WhatsApp, SMS, etc.)
4. **Driver clicks link** → Opens Telegram app
5. **Driver clicks "START"** in bot chat
6. **Backend automatically:**
   - Saves driver's Telegram Chat ID
   - Enables notifications (`TelegramNotificationsEnabled = true`)
   - Clears registration token
7. **Driver starts receiving notifications** for TODAY's rides

---

## 📊 Driver Entity Fields (for API responses)

When you fetch driver data, these new fields are available:

```typescript
interface Driver {
  id: string;
  // ... existing fields ...
  
  // New Telegram fields (all nullable)
  telegramChatId?: number;                    // Telegram Chat ID (if registered)
  telegramNotificationsEnabled: boolean;      // Whether notifications are active
  telegramRegisteredAt?: string;              // ISO timestamp of registration
  telegramRegistrationToken?: string;         // Current token (24hr expiry)
  telegramTokenExpiresAt?: string;            // Token expiration timestamp
}
```

**Note:** `telegramRegistrationToken` and `telegramTokenExpiresAt` are usually `null` (cleared after registration or expiry).

---

## 🔔 Notification Triggers (Automatic - No Frontend Action Needed)

These events **automatically** send Telegram notifications to drivers:

| Event                                  | API Endpoint                      | Notification Type           |
|----------------------------------------|-----------------------------------|-----------------------------|
| Driver assigned to TODAY's ride        | `PUT /rides/{id}/assign`          | "Nieuwe rit toegewezen"     |
| Second driver added to TODAY's ride    | `POST /rides/{id}/second-driver`  | "Nieuwe rit" + "Tweede chauffeur toegevoegd" |
| Second driver removed from TODAY's ride| `DELETE /rides/{id}/second-driver`| "Verwijderd van rit"        |
| TODAY's ride details updated           | `PUT /rides/{id}/details`         | "Rit gewijzigd"             |
| TODAY's ride trip number updated       | `PUT /rides/{id}/trip-number`     | "Rit gewijzigd"             |
| TODAY's ride deleted                   | `DELETE /rides/{id}`              | "Rit geannuleerd"           |

**Important:** Only rides with `PlannedDate = TODAY` trigger notifications.

---

## 🎨 UI/UX Recommendations

### 1. Driver List Page
Add a badge to each driver row:
```jsx
{driver.telegramNotificationsEnabled ? (
  <span className="badge badge-success">📱 Telegram</span>
) : (
  <span className="badge badge-secondary">📱 Uit</span>
)}
```

### 2. Driver Details Page
See the full component example above. Key elements:
- Status badge (active/inactive)
- Generate link button (if inactive)
- QR code display (after generation)
- Disable button (if active)
- Expiry countdown timer (optional, nice-to-have)

### 3. Settings Page (Optional)
Create a "Telegram Notificaties" section in global settings:
- Bot username display
- Test message sender (for admins)
- Bulk activation tool (future enhancement)

---

## 🐛 Error Handling

### Common Errors

#### 404 - Driver Not Found
```json
{
  "success": false,
  "message": "Driver not found."
}
```
**Frontend Action:** Show error toast, check `driverId` is valid

#### 400 - Driver Not Registered
When calling `POST /telegram/test`:
```json
{
  "success": false,
  "message": "Driver does not have Telegram enabled."
}
```
**Frontend Action:** Show error: "Chauffeur heeft Telegram niet geactiveerd"

#### 403 - Unauthorized
```json
{
  "success": false,
  "message": "Access denied."
}
```
**Frontend Action:** Redirect to login or show permission error

---

## 🧪 Testing Checklist for Frontend

- [ ] Display driver's Telegram status (active/inactive) in driver list
- [ ] Show "Genereer Activatielink" button in driver details
- [ ] Display registration URL and QR code after generation
- [ ] Copy-to-clipboard button works for registration URL
- [ ] Show expiry time for registration link
- [ ] "Uitschakelen" button works and shows confirmation dialog
- [ ] Status badge updates after enable/disable actions
- [ ] Test message button (for global admins only)
- [ ] Handle 404/400/403 errors gracefully
- [ ] Mobile responsive (QR code should be scannable)

---

## 📞 Need Help?

If you encounter issues:
1. Check browser console for errors
2. Verify API token has correct roles
3. Test with Postman/Insomnia first
4. Check backend logs: `docker logs truckmanagement-truckmanagement-1`
5. Contact backend team for webhook issues

---

**Happy Coding! 🚀**

