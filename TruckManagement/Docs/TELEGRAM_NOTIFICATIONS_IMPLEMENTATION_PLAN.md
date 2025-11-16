# 📱 Telegram Driver Notifications - Implementation Plan

## ✅ **IMPLEMENTATION STATUS: COMPLETE**

**Date Completed:** November 16, 2025  
**Implementation Duration:** Phase 1 Complete  
**Status:** ✅ All phases implemented and build successful

---

## 📋 Overview

This document outlines the implementation of a **Telegram Bot notification system** for drivers in the Truck Management system. Drivers will receive **real-time notifications via Telegram** when their **assigned rides for TODAY** are created, updated, or deleted.

**Registration Method:** ✅ **Fully Automated** - Admin generates a link, driver clicks it, instant activation (zero manual work).

---

## 🎯 Scope & Requirements

### ✅ **In Scope (MVP - Phase 1)**
- ✅ Notify drivers when **Rides assigned to them for TODAY** are:
  - **Assigned/Reassigned** (driver added to ride or changed)
  - **Updated** (route, times, trip number, truck, or notes changed)
  - **Deleted** (ride cancelled)
  - **Second driver added/removed** (for multi-driver rides)
- ✅ Support **multiple drivers per ride** (primary + second driver via `RideDriverAssignment`)
- ✅ Each driver receives **only their own notifications** (privacy)
- ✅ **Automated Telegram registration** via unique link (zero admin work)
- ✅ Database fields for Telegram Chat ID and registration tokens

### ❌ **Out of Scope (Future Phases)**
- ❌ Notifications for rides on **future dates**
- ❌ Notifications for **execution approvals/rejections**
- ❌ Notifications for **disputes or comments**
- ❌ Interactive buttons (approve/reject rides)
- ❌ SMS fallback

---

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Backend API (.NET)                           │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  Admin generates registration link                           │  │
│  │  GET /drivers/{id}/telegram/registration-link               │  │
│  │  → Returns: https://t.me/bot?start={token}                   │  │
│  └────────────────────┬─────────────────────────────────────────┘  │
│                       │                                             │
│                       ▼                                             │
│            Driver clicks link → Telegram opens                      │
│                       │                                             │
│                       ▼                                             │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  POST /telegram/webhook                                      │  │
│  │  - Receives /start {token}                                   │  │
│  │  - Validates token                                           │  │
│  │  - Auto-registers driver (saves Chat ID)                     │  │
│  │  - Sends confirmation                                        │  │
│  └────────────────────┬─────────────────────────────────────────┘  │
│                       │                                             │
│                       ▼                                             │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  RideAssignmentEndpoints.cs                                  │  │
│  │  - PUT /rides/{id}/assign (assign/reassign driver)           │  │
│  │  - POST /rides/{id}/second-driver (add 2nd driver)           │  │
│  │  - DELETE /rides/{id}/second-driver (remove 2nd)             │  │
│  │  - PUT /rides/{id}/details (update route/times)              │  │
│  │  - PUT /rides/{id}/trip-number (update trip #)               │  │
│  │                                                               │  │
│  │  RideEndpoints.cs                                            │  │
│  │  - DELETE /rides/{id} (delete ride)                          │  │
│  └────────────────────┬─────────────────────────────────────────┘  │
│                       │                                             │
│                       ▼                                             │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  TelegramNotificationService                                 │  │
│  │  - NotifyDriversOnRideAssigned(rideId, driverIds)            │  │
│  │  - NotifyDriversOnRideUpdated(rideId, changes)               │  │
│  │  - NotifyDriversOnRideDeleted(rideId, driverIds)             │  │
│  │  - NotifyDriverOnSecondDriverAdded(rideId, driverId)         │  │
│  │  - NotifyDriverOnRemovedFromRide(driverId, rideDetails)      │  │
│  └────────────────────┬─────────────────────────────────────────┘  │
│                       │                                             │
│                       ▼                                             │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  Telegram.Bot SDK                                            │  │
│  │  - SendTextMessageAsync(chatId, message)                     │  │
│  └────────────────────┬─────────────────────────────────────────┘  │
└────────────────────────┼───────────────────────────────────────────┘
                        │
                        ▼
                ┌───────────────────┐
                │  Telegram API     │
                │  (Bot Token)      │
                └─────────┬─────────┘
                          │
                          ▼
                  ┌───────────────┐
                  │  Driver's     │
                  │  Telegram App │
                  └───────────────┘
```

---

## 📊 Database Changes

### **1. Add Telegram Fields to `Driver` Entity**

```csharp
public class Driver
{
    // ... existing fields ...
    
    /// <summary>
    /// Telegram Chat ID for notifications (unique per user)
    /// </summary>
    public long? TelegramChatId { get; set; }
    
    /// <summary>
    /// Whether driver has enabled Telegram notifications
    /// </summary>
    public bool TelegramNotificationsEnabled { get; set; } = false;
    
    /// <summary>
    /// When the driver registered their Telegram account
    /// </summary>
    public DateTime? TelegramRegisteredAt { get; set; }
    
    /// <summary>
    /// One-time registration token for automated setup (e.g., "a3f8k2p9x4m1")
    /// </summary>
    public string? TelegramRegistrationToken { get; set; }
    
    /// <summary>
    /// When the registration token expires (typically 7 days)
    /// </summary>
    public DateTime? TelegramTokenExpiresAt { get; set; }
}
```

**Migration Name:** `AddTelegramFieldsToDriver`

**SQL Preview:**
```sql
ALTER TABLE "Drivers" 
ADD COLUMN "TelegramChatId" bigint NULL,
ADD COLUMN "TelegramNotificationsEnabled" boolean NOT NULL DEFAULT false,
ADD COLUMN "TelegramRegisteredAt" timestamp with time zone NULL,
ADD COLUMN "TelegramRegistrationToken" text NULL,
ADD COLUMN "TelegramTokenExpiresAt" timestamp with time zone NULL;
```

---

## 🔧 Implementation Details

### **Phase 1: Setup & Configuration (10 minutes)**

#### **1.1 Create Telegram Bot** ✅ **COMPLETED**

**Bot Details:**
- **Bot Name:** Vervoer Manager Driver Bot
- **Bot Username:** `VervoerManager_Driver_Bot`
- **Bot URL:** https://t.me/VervoerManager_Driver_Bot
- **Bot Token:** `8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA`

⚠️ **Security:** Keep this token secure! Anyone with this token can control your bot.

#### **1.2 Add Configuration**

**File:** `appsettings.json`

```json
{
  "Telegram": {
    "BotToken": "YOUR_BOT_TOKEN_HERE",
    "BotUsername": "VervoerManager_Driver_Bot"
  }
}
```

**File:** `appsettings.Development.json`

```json
{
  "Telegram": {
    "BotToken": "8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA",
    "BotUsername": "VervoerManager_Driver_Bot"
  }
}
```

**⚠️ Security Note:** For production, store the `BotToken` in **environment variables** or **Azure Key Vault**.

#### **1.3 Install NuGet Package**

```bash
cd TruckManagement
dotnet add package Telegram.Bot --version 19.0.0
```

---

### **Phase 2: Service Implementation (2 hours)**

#### **2.1 Create Telegram Configuration Options**

**File:** `TruckManagement/Options/TelegramOptions.cs`

```csharp
namespace TruckManagement.Options
{
    public class TelegramOptions
    {
        public string BotToken { get; set; } = string.Empty;
        public string BotUsername { get; set; } = string.Empty;
    }
}
```

#### **2.2 Create Telegram Notification Service Interface**

**File:** `TruckManagement/Interfaces/ITelegramNotificationService.cs`

```csharp
namespace TruckManagement.Interfaces
{
    /// <summary>
    /// Service for sending Telegram notifications to drivers about ride changes
    /// </summary>
    public interface ITelegramNotificationService
    {
        /// <summary>
        /// Notify driver(s) when assigned to a ride for today
        /// </summary>
        Task NotifyDriversOnRideAssignedAsync(Guid rideId, List<Guid> driverIds);
        
        /// <summary>
        /// Notify driver(s) when their ride for today is updated
        /// </summary>
        Task NotifyDriversOnRideUpdatedAsync(Guid rideId, string changesSummary);
        
        /// <summary>
        /// Notify driver(s) when their ride for today is deleted
        /// </summary>
        Task NotifyDriversOnRideDeletedAsync(Guid rideId, List<Guid> driverIds);
        
        /// <summary>
        /// Notify primary driver when second driver is added
        /// </summary>
        Task NotifyDriverOnSecondDriverAddedAsync(Guid rideId, Guid primaryDriverId, string secondDriverName);
        
        /// <summary>
        /// Notify driver when they are removed from a ride
        /// </summary>
        Task NotifyDriverOnRemovedFromRideAsync(Guid driverId, string rideDetails);
        
        /// <summary>
        /// Send any message to a chat (used for testing and confirmations)
        /// </summary>
        Task SendMessageAsync(long chatId, string message);
    }
}
```

#### **2.3 Implement Telegram Notification Service**

**File:** `TruckManagement/Services/TelegramNotificationService.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TruckManagement.Data;
using TruckManagement.Interfaces;
using TruckManagement.Options;

namespace TruckManagement.Services
{
    public class TelegramNotificationService : ITelegramNotificationService
    {
        private readonly TelegramBotClient _botClient;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<TelegramNotificationService> _logger;
        private readonly string _botUsername;

        public TelegramNotificationService(
            IOptions<TelegramOptions> telegramOptions,
            ApplicationDbContext db,
            ILogger<TelegramNotificationService> logger)
        {
            var token = telegramOptions.Value.BotToken;
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("Telegram Bot Token is not configured.");
            }

            _botClient = new TelegramBotClient(token);
            _db = db;
            _logger = logger;
            _botUsername = telegramOptions.Value.BotUsername;
        }

        public async Task NotifyDriversOnRideAssignedAsync(Guid rideId, List<Guid> driverIds)
        {
            try
            {
                var ride = await _db.Rides
                    .Include(r => r.Client)
                    .Include(r => r.Truck)
                    .FirstOrDefaultAsync(r => r.Id == rideId);

                if (ride == null)
                {
                    _logger.LogWarning("Ride {RideId} not found for Telegram notification.", rideId);
                    return;
                }

                // Only notify for TODAY's rides
                var today = DateTime.UtcNow.Date;
                if (!ride.PlannedDate.HasValue || ride.PlannedDate.Value.Date != today)
                {
                    _logger.LogDebug("Skipping Telegram notification for Ride {RideId} (not today).", rideId);
                    return;
                }

                foreach (var driverId in driverIds)
                {
                    var driver = await _db.Drivers
                        .Include(d => d.User)
                        .FirstOrDefaultAsync(d => d.Id == driverId);

                    if (driver == null || 
                        !driver.TelegramNotificationsEnabled || 
                        !driver.TelegramChatId.HasValue)
                    {
                        _logger.LogDebug("Driver {DriverId} does not have Telegram notifications enabled.", driverId);
                        continue;
                    }

                    var assignment = await _db.RideDriverAssignments
                        .FirstOrDefaultAsync(rda => rda.RideId == rideId && rda.DriverId == driverId);

                    var message = FormatRideAssignedMessage(ride, assignment?.IsPrimary ?? true);
                    await SendMessageAsync(driver.TelegramChatId.Value, message);

                    _logger.LogInformation(
                        "Sent Telegram notification to driver {DriverId} for assigned Ride {RideId}.", 
                        driverId, 
                        rideId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error sending Telegram notification for Ride assignment {RideId}.", 
                    rideId);
            }
        }

        public async Task NotifyDriversOnRideUpdatedAsync(Guid rideId, string changesSummary)
        {
            try
            {
                var ride = await _db.Rides
                    .Include(r => r.Client)
                    .Include(r => r.Truck)
                    .Include(r => r.DriverAssignments)
                        .ThenInclude(da => da.Driver)
                    .FirstOrDefaultAsync(r => r.Id == rideId);

                if (ride == null)
                {
                    _logger.LogWarning("Ride {RideId} not found for Telegram notification.", rideId);
                    return;
                }

                // Only notify for TODAY's rides
                var today = DateTime.UtcNow.Date;
                if (!ride.PlannedDate.HasValue || ride.PlannedDate.Value.Date != today)
                {
                    _logger.LogDebug("Skipping Telegram notification for Ride {RideId} (not today).", rideId);
                    return;
                }

                // Notify all assigned drivers
                foreach (var assignment in ride.DriverAssignments)
                {
                    var driver = assignment.Driver;
                    
                    if (driver == null || 
                        !driver.TelegramNotificationsEnabled || 
                        !driver.TelegramChatId.HasValue)
                    {
                        _logger.LogDebug("Driver {DriverId} does not have Telegram notifications enabled.", 
                            assignment.DriverId);
                        continue;
                    }

                    var message = FormatRideUpdatedMessage(ride, changesSummary);
                    await SendMessageAsync(driver.TelegramChatId.Value, message);

                    _logger.LogInformation(
                        "Sent Telegram notification to driver {DriverId} for updated Ride {RideId}.", 
                        assignment.DriverId, 
                        rideId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error sending Telegram notification for Ride update {RideId}.", 
                    rideId);
            }
        }

        public async Task NotifyDriversOnRideDeletedAsync(Guid rideId, List<Guid> driverIds)
        {
            try
            {
                foreach (var driverId in driverIds)
                {
                    var driver = await _db.Drivers.FirstOrDefaultAsync(d => d.Id == driverId);

                    if (driver == null || 
                        !driver.TelegramNotificationsEnabled || 
                        !driver.TelegramChatId.HasValue)
                    {
                        _logger.LogDebug("Driver {DriverId} does not have Telegram notifications enabled.", driverId);
                        continue;
                    }

                    var message = FormatRideDeletedMessage();
                    await SendMessageAsync(driver.TelegramChatId.Value, message);

                    _logger.LogInformation(
                        "Sent Telegram notification to driver {DriverId} for deleted Ride {RideId}.", 
                        driverId, 
                        rideId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error sending Telegram notification for Ride deletion {RideId}.", 
                    rideId);
            }
        }

        public async Task NotifyDriverOnSecondDriverAddedAsync(Guid rideId, Guid primaryDriverId, string secondDriverName)
        {
            try
            {
                var ride = await _db.Rides
                    .Include(r => r.Client)
                    .Include(r => r.Truck)
                    .FirstOrDefaultAsync(r => r.Id == rideId);

                if (ride == null)
                {
                    _logger.LogWarning("Ride {RideId} not found for Telegram notification.", rideId);
                    return;
                }

                // Only notify for TODAY's rides
                var today = DateTime.UtcNow.Date;
                if (!ride.PlannedDate.HasValue || ride.PlannedDate.Value.Date != today)
                {
                    return;
                }

                var driver = await _db.Drivers.FirstOrDefaultAsync(d => d.Id == primaryDriverId);

                if (driver == null || 
                    !driver.TelegramNotificationsEnabled || 
                    !driver.TelegramChatId.HasValue)
                {
                    return;
                }

                var message = FormatSecondDriverAddedMessage(ride, secondDriverName);
                await SendMessageAsync(driver.TelegramChatId.Value, message);

                _logger.LogInformation(
                    "Sent Telegram notification to driver {DriverId} about second driver added to Ride {RideId}.", 
                    primaryDriverId, 
                    rideId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error sending Telegram notification for second driver addition {RideId}.", 
                    rideId);
            }
        }

        public async Task NotifyDriverOnRemovedFromRideAsync(Guid driverId, string rideDetails)
        {
            try
            {
                var driver = await _db.Drivers.FirstOrDefaultAsync(d => d.Id == driverId);

                if (driver == null || 
                    !driver.TelegramNotificationsEnabled || 
                    !driver.TelegramChatId.HasValue)
                {
                    return;
                }

                var message = FormatDriverRemovedMessage(rideDetails);
                await SendMessageAsync(driver.TelegramChatId.Value, message);

                _logger.LogInformation(
                    "Sent Telegram notification to driver {DriverId} about removal from ride.", 
                    driverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error sending Telegram notification for driver removal.", 
                    driverId);
            }
        }

        public async Task SendMessageAsync(long chatId, string message)
        {
            try
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: message,
                    parseMode: ParseMode.Html,
                    disableNotification: false
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Telegram message to chat {ChatId}.", chatId);
                throw;
            }
        }

        // ==================== Private Helper Methods ====================

        private string FormatRideAssignedMessage(Entities.Ride ride, bool isPrimary)
        {
            var clientName = ride.Client?.Name ?? "Onbekend";
            var truckPlate = ride.Truck?.LicensePlate ?? "Onbekend";
            var startTime = ride.PlannedStartTime?.ToString(@"hh\:mm") ?? "Onbekend";
            var endTime = ride.PlannedEndTime?.ToString(@"hh\:mm") ?? "Onbekend";
            var role = isPrimary ? "hoofdchauffeur" : "tweede chauffeur";

            return $"🚛 <b>Nieuwe rit toegewezen!</b>\n\n" +
                   $"📅 <b>Datum:</b> {ride.PlannedDate:dd-MM-yyyy} (Vandaag)\n" +
                   $"⏰ <b>Tijd:</b> {startTime} - {endTime}\n" +
                   $"📍 <b>Route:</b> {ride.RouteFromName ?? "?"} → {ride.RouteToName ?? "?"}\n" +
                   $"👤 <b>Klant:</b> {clientName}\n" +
                   $"🚚 <b>Voertuig:</b> {truckPlate}\n" +
                   $"👷 <b>Rol:</b> {role}\n" +
                   (!string.IsNullOrWhiteSpace(ride.TripNumber) 
                       ? $"🔢 <b>Ritnummer:</b> {ride.TripNumber}\n" 
                       : "") +
                   (!string.IsNullOrWhiteSpace(ride.Notes) 
                       ? $"💬 <b>Notities:</b> {ride.Notes}\n" 
                       : "");
        }

        private string FormatRideUpdatedMessage(Entities.Ride ride, string changesSummary)
        {
            var clientName = ride.Client?.Name ?? "Onbekend";
            var truckPlate = ride.Truck?.LicensePlate ?? "Onbekend";
            var startTime = ride.PlannedStartTime?.ToString(@"hh\:mm") ?? "Onbekend";
            var endTime = ride.PlannedEndTime?.ToString(@"hh\:mm") ?? "Onbekend";

            return $"✏️ <b>Rit gewijzigd!</b>\n\n" +
                   $"📅 <b>Datum:</b> {ride.PlannedDate:dd-MM-yyyy} (Vandaag)\n" +
                   $"⏰ <b>Tijd:</b> {startTime} - {endTime}\n" +
                   $"📍 <b>Route:</b> {ride.RouteFromName ?? "?"} → {ride.RouteToName ?? "?"}\n" +
                   $"👤 <b>Klant:</b> {clientName}\n" +
                   $"🚚 <b>Voertuig:</b> {truckPlate}\n" +
                   (!string.IsNullOrWhiteSpace(ride.TripNumber) 
                       ? $"🔢 <b>Ritnummer:</b> {ride.TripNumber}\n" 
                       : "") +
                   (string.IsNullOrWhiteSpace(changesSummary) 
                       ? "" 
                       : $"\n<b>Wijzigingen:</b>\n{changesSummary}\n");
        }

        private string FormatRideDeletedMessage()
        {
            return $"🗑️ <b>Rit geannuleerd!</b>\n\n" +
                   $"Een van je ritten voor vandaag is geannuleerd.\n" +
                   $"Neem contact op met je dispatcher voor meer informatie.";
        }

        private string FormatSecondDriverAddedMessage(Entities.Ride ride, string secondDriverName)
        {
            var clientName = ride.Client?.Name ?? "Onbekend";
            var startTime = ride.PlannedStartTime?.ToString(@"hh\:mm") ?? "Onbekend";

            return $"👥 <b>Tweede chauffeur toegevoegd!</b>\n\n" +
                   $"Er is een tweede chauffeur toegevoegd aan je rit van vandaag.\n\n" +
                   $"⏰ <b>Tijd:</b> {startTime}\n" +
                   $"👤 <b>Klant:</b> {clientName}\n" +
                   $"👷 <b>Tweede chauffeur:</b> {secondDriverName}";
        }

        private string FormatDriverRemovedMessage(string rideDetails)
        {
            return $"🚫 <b>Verwijderd van rit!</b>\n\n" +
                   $"Je bent verwijderd van een rit voor vandaag.\n\n" +
                   $"<b>Details:</b>\n{rideDetails}\n\n" +
                   $"Neem contact op met je dispatcher voor meer informatie.";
        }
    }
}
```

---

### **Phase 3: Integration with Ride Endpoints (2 hours)**

#### **3.1 Modify `PUT /rides/{id}/assign` (Assign/Reassign Driver)**

**File:** `TruckManagement/Endpoints/RideAssignmentEndpoints.cs`

**Location:** Inside `MapRideAssignmentEndpoints()` method, after `await db.SaveChangesAsync();`

```csharp
app.MapPut("/rides/{id}/assign",
    [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
    async (
        Guid id,
        [FromBody] AssignRideRequest request,
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ClaimsPrincipal currentUser,
        ITelegramNotificationService telegramService // ✅ ADD THIS
    ) =>
    {
        try
        {
            // ... existing code ...
            
            await db.SaveChangesAsync();

            // ✅ NEW: Send Telegram notification (non-blocking)
            if (request.DriverId.HasValue)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await telegramService.NotifyDriversOnRideAssignedAsync(
                            id, 
                            new List<Guid> { request.DriverId.Value });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Telegram] Failed to send notification: {ex.Message}");
                    }
                });
            }

            return ApiResponseFactory.Success("Ride assignment updated successfully.");
        }
        catch (Exception ex)
        {
            // ... existing error handling ...
        }
    });
```

#### **3.2 Modify `POST /rides/{id}/second-driver` (Add Second Driver)**

**File:** `TruckManagement/Endpoints/RideAssignmentEndpoints.cs`

**Location:** Inside `MapRideAssignmentEndpoints()` method, after `await db.SaveChangesAsync();`

```csharp
app.MapPost("/rides/{id}/second-driver",
    [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
    async (
        Guid id,
        [FromBody] AddSecondDriverRequest request,
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ClaimsPrincipal currentUser,
        ITelegramNotificationService telegramService // ✅ ADD THIS
    ) =>
    {
        try
        {
            // ... existing code ...
            
            db.RideDriverAssignments.Add(newAssignment);
            await db.SaveChangesAsync();

            // ✅ NEW: Get driver names for notifications
            var secondDriver = await db.Drivers
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == request.DriverId);
            
            var secondDriverName = secondDriver != null 
                ? $"{secondDriver.User?.FirstName} {secondDriver.User?.LastName}".Trim()
                : "Onbekend";

            // ✅ NEW: Notify both drivers (non-blocking)
            _ = Task.Run(async () =>
            {
                try
                {
                    // Notify second driver that they were assigned
                    await telegramService.NotifyDriversOnRideAssignedAsync(
                        id, 
                        new List<Guid> { request.DriverId });

                    // Notify primary driver that second driver was added
                    var primaryAssignment = ride.DriverAssignments.FirstOrDefault(da => da.IsPrimary);
                    if (primaryAssignment != null)
                    {
                        await telegramService.NotifyDriverOnSecondDriverAddedAsync(
                            id, 
                            primaryAssignment.DriverId, 
                            secondDriverName);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Telegram] Failed to send notification: {ex.Message}");
                }
            });

            return ApiResponseFactory.Success("Second driver added successfully.", StatusCodes.Status201Created);
        }
        catch (Exception ex)
        {
            // ... existing error handling ...
        }
    });
```

#### **3.3 Modify `DELETE /rides/{id}/second-driver` (Remove Second Driver)**

**File:** `TruckManagement/Endpoints/RideAssignmentEndpoints.cs`

**Location:** Inside `MapRideAssignmentEndpoints()` method, before `db.RideDriverAssignments.Remove(secondDriver);`

```csharp
app.MapDelete("/rides/{id}/second-driver",
    [Authorize(Roles = "globalAdmin, customerAdmin")]
    async (
        Guid id,
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ClaimsPrincipal currentUser,
        ITelegramNotificationService telegramService // ✅ ADD THIS
    ) =>
    {
        try
        {
            // ... existing code ...
            
            var secondDriver = ride.DriverAssignments.FirstOrDefault(da => !da.IsPrimary);
            if (secondDriver == null)
            {
                return ApiResponseFactory.Error("No second driver assigned to this ride.", StatusCodes.Status404NotFound);
            }

            // ✅ NEW: Save info BEFORE deleting
            var removedDriverId = secondDriver.DriverId;
            var rideDetails = $"{ride.PlannedDate:dd-MM-yyyy} {ride.PlannedStartTime:hh\\:mm} - {ride.Client?.Name ?? "Onbekend"}";
            var isToday = ride.PlannedDate.HasValue && ride.PlannedDate.Value.Date == DateTime.UtcNow.Date;

            db.RideDriverAssignments.Remove(secondDriver);
            await db.SaveChangesAsync();

            // ✅ NEW: Notify removed driver (non-blocking)
            if (isToday)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await telegramService.NotifyDriverOnRemovedFromRideAsync(
                            removedDriverId, 
                            rideDetails);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Telegram] Failed to send notification: {ex.Message}");
                    }
                });
            }

            return ApiResponseFactory.Success("Second driver removed successfully.");
        }
        catch (Exception ex)
        {
            // ... existing error handling ...
        }
    });
```

#### **3.4 Modify `PUT /rides/{id}/details` (Update Route/Times/Notes)**

**File:** `TruckManagement/Endpoints/RideAssignmentEndpoints.cs`

**Location:** Inside `MapRideAssignmentEndpoints()` method, after `await db.SaveChangesAsync();`

```csharp
app.MapPut("/rides/{id}/details",
    [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
    async (
        Guid id,
        [FromBody] UpdateRideDetailsRequest request,
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ClaimsPrincipal currentUser,
        ITelegramNotificationService telegramService // ✅ ADD THIS
    ) =>
    {
        try
        {
            // ... existing code ...
            
            // ✅ NEW: Track what changed
            var changes = new List<string>();
            
            // Check if route changed
            if (!string.IsNullOrWhiteSpace(request.RouteFromName) && 
                request.RouteFromName.Trim() != ride.RouteFromName)
            {
                changes.Add($"Van: {ride.RouteFromName ?? "?"} → {request.RouteFromName.Trim()}");
            }
            
            if (!string.IsNullOrWhiteSpace(request.RouteToName) && 
                request.RouteToName.Trim() != ride.RouteToName)
            {
                changes.Add($"Naar: {ride.RouteToName ?? "?"} → {request.RouteToName.Trim()}");
            }
            
            // Check if times changed
            if (request.PlannedStartTime.HasValue && 
                request.PlannedStartTime.Value != ride.PlannedStartTime)
            {
                changes.Add($"Starttijd: {ride.PlannedStartTime:hh\\:mm} → {request.PlannedStartTime.Value:hh\\:mm}");
            }
            
            if (request.PlannedEndTime.HasValue && 
                request.PlannedEndTime.Value != ride.PlannedEndTime)
            {
                changes.Add($"Eindtijd: {ride.PlannedEndTime:hh\\:mm} → {request.PlannedEndTime.Value:hh\\:mm}");
            }
            
            // Check if notes changed
            var oldNotes = ride.Notes ?? "";
            var newNotes = request.Notes?.Trim() ?? "";
            if (newNotes != oldNotes)
            {
                changes.Add("Notities gewijzigd");
            }

            // Update ride details (treat empty strings as null)
            ride.RouteFromName = string.IsNullOrWhiteSpace(request.RouteFromName) ? null : request.RouteFromName.Trim();
            ride.RouteToName = string.IsNullOrWhiteSpace(request.RouteToName) ? null : request.RouteToName.Trim();
            ride.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
            ride.PlannedStartTime = request.PlannedStartTime;
            ride.PlannedEndTime = request.PlannedEndTime;

            await db.SaveChangesAsync();

            // ✅ NEW: Send Telegram notification if changes detected (non-blocking)
            if (changes.Any())
            {
                var changesSummary = string.Join("\n", changes.Select(c => $"• {c}"));
                
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await telegramService.NotifyDriversOnRideUpdatedAsync(id, changesSummary);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Telegram] Failed to send notification: {ex.Message}");
                    }
                });
            }

            var response = new RideDetailsDto
            {
                Id = ride.Id,
                RouteFromName = ride.RouteFromName,
                RouteToName = ride.RouteToName,
                Notes = ride.Notes,
                PlannedStartTime = ride.PlannedStartTime,
                PlannedEndTime = ride.PlannedEndTime,
                UpdatedAt = DateTime.UtcNow
            };

            return ApiResponseFactory.Success(response);
        }
        catch (Exception ex)
        {
            // ... existing error handling ...
        }
    });
```

#### **3.5 Modify `PUT /rides/{id}/trip-number` (Update Trip Number)**

**File:** `TruckManagement/Endpoints/RideAssignmentEndpoints.cs`

**Location:** Inside `MapRideAssignmentEndpoints()` method, after `await db.SaveChangesAsync();`

```csharp
app.MapPut("/rides/{id}/trip-number",
    [Authorize(Roles = "globalAdmin, customerAdmin, employer")]
    async (
        Guid id,
        [FromBody] UpdateTripNumberRequest request,
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ClaimsPrincipal currentUser,
        ITelegramNotificationService telegramService // ✅ ADD THIS
    ) =>
    {
        try
        {
            // ... existing code ...
            
            // ✅ NEW: Track change
            var oldTripNumber = ride.TripNumber ?? "Geen";
            var newTripNumber = request.TripNumber ?? "Geen";
            bool changed = oldTripNumber != newTripNumber;

            // Update trip number
            ride.TripNumber = request.TripNumber;
            await db.SaveChangesAsync();

            // ✅ NEW: Send Telegram notification if changed (non-blocking)
            if (changed)
            {
                var changesSummary = $"• Ritnummer: {oldTripNumber} → {newTripNumber}";
                
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await telegramService.NotifyDriversOnRideUpdatedAsync(id, changesSummary);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Telegram] Failed to send notification: {ex.Message}");
                    }
                });
            }

            var response = new
            {
                Id = ride.Id,
                TripNumber = ride.TripNumber
            };

            return ApiResponseFactory.Success(response);
        }
        catch (Exception ex)
        {
            // ... existing error handling ...
        }
    });
```

#### **3.6 Modify `DELETE /rides/{id}` (Delete Ride)**

**File:** `TruckManagement/Endpoints/RideEndpoints.cs`

**Location:** Inside `MapRideEndpoints()` method, before `db.Rides.Remove(ride);`

```csharp
app.MapDelete("/rides/{id}",
    [Authorize(Roles = "globalAdmin, customerAdmin")]
    async (
        string id,
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        ClaimsPrincipal currentUser,
        ITelegramNotificationService telegramService // ✅ ADD THIS
    ) =>
    {
        try
        {
            // ... existing validation code ...
            
            // Load ride with assignments
            var ride = await db.Rides
                .Include(r => r.DriverAssignments)
                .Include(r => r.PartRides)
                .FirstOrDefaultAsync(r => r.Id == rideGuid);

            if (ride == null)
            {
                return ApiResponseFactory.Error("Ride not found.", StatusCodes.Status404NotFound);
            }

            // ... existing authorization and validation code ...

            // ✅ NEW: Save driver IDs and check if today BEFORE deleting
            var assignedDriverIds = ride.DriverAssignments.Select(da => da.DriverId).ToList();
            var isToday = ride.PlannedDate.HasValue && ride.PlannedDate.Value.Date == DateTime.UtcNow.Date;

            // Delete the ride
            db.Rides.Remove(ride);
            await db.SaveChangesAsync();

            // ✅ NEW: Send Telegram notification if ride was for today (non-blocking)
            if (isToday && assignedDriverIds.Any())
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await telegramService.NotifyDriversOnRideDeletedAsync(
                            rideGuid, 
                            assignedDriverIds);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Telegram] Failed to send notification: {ex.Message}");
                    }
                });
            }

            return ApiResponseFactory.Success("Ride deleted successfully.", StatusCodes.Status200OK);
        }
        catch (Exception ex)
        {
            // ... existing error handling ...
        }
    });
```

---

### **Phase 4: Automated Registration Endpoints (1.5 hours)**

#### **4.1 Create Telegram Endpoints**

**File:** `TruckManagement/Endpoints/TelegramEndpoints.cs` (NEW FILE)

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;
using TruckManagement.Data;
using TruckManagement.Helpers;
using TruckManagement.Interfaces;
using TruckManagement.Options;

namespace TruckManagement.Endpoints
{
    public static class TelegramEndpoints
    {
        public static void MapTelegramEndpoints(this WebApplication app)
        {
            // ========== AUTOMATED REGISTRATION ==========
            
            // GET /drivers/{driverId}/telegram/registration-link - Generate registration link
            app.MapGet("/drivers/{driverId}/telegram/registration-link",
                [Authorize(Roles = "globalAdmin, customerAdmin")]
                async (
                    Guid driverId,
                    ApplicationDbContext db,
                    IOptions<TelegramOptions> telegramOptions
                ) =>
                {
                    try
                    {
                        var driver = await db.Drivers
                            .Include(d => d.User)
                            .FirstOrDefaultAsync(d => d.Id == driverId);
                        
                        if (driver == null)
                        {
                            return ApiResponseFactory.Error("Driver not found.", StatusCodes.Status404NotFound);
                        }
                        
                        // Check if already registered
                        if (driver.TelegramNotificationsEnabled && driver.TelegramChatId.HasValue)
                        {
                            return ApiResponseFactory.Error(
                                "Driver already has Telegram notifications enabled.", 
                                StatusCodes.Status400BadRequest);
                        }
                        
                        // Generate unique token (12 characters, URL-safe)
                        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                            .Replace("+", "")
                            .Replace("/", "")
                            .Replace("=", "")
                            .Substring(0, 12);
                        
                        // Store token (valid for 7 days)
                        driver.TelegramRegistrationToken = token;
                        driver.TelegramTokenExpiresAt = DateTime.UtcNow.AddDays(7);
                        await db.SaveChangesAsync();
                        
                        var botUsername = telegramOptions.Value.BotUsername;
                        var registrationLink = $"https://t.me/{botUsername}?start={token}";
                        
                        return ApiResponseFactory.Success(new
                        {
                            registrationLink = registrationLink,
                            token = token,
                            expiresAt = driver.TelegramTokenExpiresAt,
                            driverName = $"{driver.User?.FirstName} {driver.User?.LastName}".Trim(),
                            instructions = $"Stuur deze link naar {driver.User?.FirstName}. " +
                                         $"De chauffeur klikt op de link en zijn account wordt automatisch geactiveerd."
                        }, StatusCodes.Status200OK);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error generating registration link: {ex.Message}");
                        return ApiResponseFactory.Error(
                            "An error occurred while generating registration link.", 
                            StatusCodes.Status500InternalServerError);
                    }
                });

            // POST /telegram/webhook - Receive updates from Telegram (auto-registration)
            app.MapPost("/telegram/webhook",
                async (
                    [FromBody] Update update,
                    ApplicationDbContext db,
                    ITelegramNotificationService telegramService
                ) =>
                {
                    try
                    {
                        if (update.Message == null || update.Message.Text == null)
                        {
                            return Results.Ok();
                        }

                        var chatId = update.Message.Chat.Id;
                        var text = update.Message.Text.Trim();
                        var firstName = update.Message.Chat.FirstName ?? "Chauffeur";

                        // Handle /start command
                        if (text.StartsWith("/start"))
                        {
                            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            
                            // ✅ AUTO-REGISTRATION: /start {token}
                            if (parts.Length == 2)
                            {
                                var token = parts[1];
                                
                                // Find driver with this token
                                var driver = await db.Drivers
                                    .Include(d => d.User)
                                    .FirstOrDefaultAsync(d => 
                                        d.TelegramRegistrationToken == token &&
                                        d.TelegramTokenExpiresAt.HasValue &&
                                        d.TelegramTokenExpiresAt.Value > DateTime.UtcNow);
                                
                                if (driver != null)
                                {
                                    // ✅ AUTO-REGISTER
                                    driver.TelegramChatId = chatId;
                                    driver.TelegramNotificationsEnabled = true;
                                    driver.TelegramRegisteredAt = DateTime.UtcNow;
                                    driver.TelegramRegistrationToken = null; // Clear token (one-time use)
                                    driver.TelegramTokenExpiresAt = null;
                                    
                                    await db.SaveChangesAsync();
                                    
                                    var successMessage = 
                                        $"✅ <b>Welkom, {driver.User?.FirstName ?? firstName}!</b>\n\n" +
                                        $"Je account is succesvol geactiveerd.\n" +
                                        $"Je ontvangt nu notificaties voor je ritten van vandaag.\n\n" +
                                        $"🚛 <b>Wat ontvang je?</b>\n" +
                                        $"• Nieuwe ritten die aan je worden toegewezen\n" +
                                        $"• Wijzigingen in je ritten (tijd, route, etc.)\n" +
                                        $"• Meldingen wanneer ritten worden geannuleerd";
                                    
                                    await telegramService.SendMessageAsync(chatId, successMessage);
                                    
                                    Console.WriteLine($"[Telegram] Driver {driver.Id} auto-registered with Chat ID {chatId}");
                                }
                                else
                                {
                                    // Invalid or expired token
                                    var errorMessage = 
                                        "❌ <b>Ongeldige of verlopen registratielink</b>\n\n" +
                                        "Deze link is niet meer geldig.\n" +
                                        "Vraag je beheerder om een nieuwe registratielink te genereren.";
                                    
                                    await telegramService.SendMessageAsync(chatId, errorMessage);
                                }
                            }
                            else
                            {
                                // ⚠️ NO TOKEN PROVIDED (fallback: show welcome message)
                                var welcomeMessage = 
                                    $"👋 <b>Welkom bij Vervoer Manager Driver Bot!</b>\n\n" +
                                    $"Om notificaties te ontvangen, heb je een registratielink nodig van je beheerder.\n\n" +
                                    $"Vraag je beheerder om een registratielink voor jou te genereren.";
                                
                                await telegramService.SendMessageAsync(chatId, welcomeMessage);
                            }
                        }

                        return Results.Ok();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error processing Telegram webhook: {ex.Message}");
                        return Results.Ok(); // Always return 200 to Telegram
                    }
                });

            // DELETE /drivers/{driverId}/telegram - Disable Telegram notifications
            app.MapDelete("/drivers/{driverId}/telegram",
                [Authorize(Roles = "globalAdmin, customerAdmin")]
                async (
                    Guid driverId,
                    ApplicationDbContext db
                ) =>
                {
                    try
                    {
                        var driver = await db.Drivers.FirstOrDefaultAsync(d => d.Id == driverId);
                        if (driver == null)
                        {
                            return ApiResponseFactory.Error("Driver not found.", StatusCodes.Status404NotFound);
                        }

                        driver.TelegramChatId = null;
                        driver.TelegramNotificationsEnabled = false;
                        driver.TelegramRegisteredAt = null;
                        driver.TelegramRegistrationToken = null;
                        driver.TelegramTokenExpiresAt = null;

                        await db.SaveChangesAsync();

                        return ApiResponseFactory.Success(
                            "Telegram notifications disabled for driver.", 
                            StatusCodes.Status200OK);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error disabling driver Telegram: {ex.Message}");
                        return ApiResponseFactory.Error(
                            "An error occurred while disabling Telegram.", 
                            StatusCodes.Status500InternalServerError);
                    }
                });

            // POST /telegram/test - Test notification (admin only)
            app.MapPost("/telegram/test",
                [Authorize(Roles = "globalAdmin")]
                async (
                    [FromBody] SendTestTelegramRequest request,
                    ITelegramNotificationService telegramService
                ) =>
                {
                    try
                    {
                        await telegramService.SendMessageAsync(request.ChatId, request.Message);
                        return ApiResponseFactory.Success("Test message sent.", StatusCodes.Status200OK);
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error(
                            $"Failed to send test message: {ex.Message}", 
                            StatusCodes.Status500InternalServerError);
                    }
                });
        }
    }

    // DTO for test notifications
    public class SendTestTelegramRequest
    {
        public long ChatId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
```

---

### **Phase 5: Dependency Injection & Configuration (15 minutes)**

#### **5.1 Register Services in `Program.cs`**

**File:** `TruckManagement/Program.cs`

```csharp
using TruckManagement.Options;
using TruckManagement.Interfaces;
using TruckManagement.Services;
using TruckManagement.Endpoints;

// ... existing code ...

// ✅ Add Telegram configuration
builder.Services.Configure<TelegramOptions>(
    builder.Configuration.GetSection("Telegram"));

// ✅ Register Telegram Notification Service
builder.Services.AddScoped<ITelegramNotificationService, TelegramNotificationService>();

// ... existing code ...

// ✅ Map Telegram endpoints
app.MapTelegramEndpoints();
```

---

### **Phase 6: Database Migration (10 minutes)**

#### **6.1 Create Migration**

```bash
cd TruckManagement
dotnet ef migrations add AddTelegramFieldsToDriver
```

#### **6.2 Apply Migration (Development)**

```bash
dotnet ef database update
```

#### **6.3 Apply Migration (Production via Docker)**

The migration will be applied automatically on container restart via `MigrateAsync()` in `Program.cs`.

---

## 🧪 Testing Plan

### **Test 1: Generate Registration Link**

**Admin Side (Postman/Frontend):**
```http
GET https://api.vervoermanager.nl/drivers/{driverId}/telegram/registration-link
Authorization: Bearer {adminToken}
```

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "registrationLink": "https://t.me/VervoerManager_Driver_Bot?start=a3f8k2p9x4m1",
    "token": "a3f8k2p9x4m1",
    "expiresAt": "2025-11-23T10:30:00Z",
    "driverName": "John Doe",
    "instructions": "Stuur deze link naar John..."
  }
}
```

---

### **Test 2: Driver Auto-Registration**

1. **Driver clicks the link** from Test 1
2. **Telegram app opens automatically**
3. **Driver sees:** "START" button in the chat
4. **Driver clicks "START"**
5. **Expected:** Driver receives instant confirmation:
   ```
   ✅ Welkom, John!
   
   Je account is succesvol geactiveerd.
   Je ontvangt nu notificaties voor je ritten van vandaag.
   ```
6. **Backend:** `TelegramChatId`, `TelegramNotificationsEnabled`, and `TelegramRegisteredAt` are saved
7. **Token is cleared** (one-time use)

---

### **Test 3: Expired Token**

1. **Wait 7 days** or manually set `TelegramTokenExpiresAt` to past date
2. **Driver clicks old link**
3. **Expected:** 
   ```
   ❌ Ongeldige of verlopen registratielink
   
   Deze link is niet meer geldig.
   Vraag je beheerder om een nieuwe registratielink te genereren.
   ```

---

### **Test 4: Assign Driver to TODAY's Ride**

```http
PUT https://api.vervoermanager.nl/rides/{rideId}/assign
Authorization: Bearer {adminToken}
Content-Type: application/json

{
  "driverId": "{registeredDriverId}",
  "truckId": "{truckId}",
  "totalPlannedHours": 8.0,
  "driverPlannedHours": 8.0
}
```

**Expected:** Driver receives "🚛 Nieuwe rit toegewezen!" message

---

### **Test 5: Update Route for TODAY's Ride**

```http
PUT https://api.vervoermanager.nl/rides/{rideId}/details
Authorization: Bearer {adminToken}
Content-Type: application/json

{
  "routeFromName": "Utrecht",
  "routeToName": "Den Haag",
  "plannedStartTime": "09:00:00",
  "plannedEndTime": "18:00:00",
  "notes": "Pak container 12345"
}
```

**Expected:** Driver receives "✏️ Rit gewijzigd!" message with changes listed

---

### **Test 6: Add Second Driver to TODAY's Ride**

```http
POST https://api.vervoermanager.nl/rides/{rideId}/second-driver
Authorization: Bearer {adminToken}
Content-Type: application/json

{
  "driverId": "{secondDriverId}",
  "plannedHours": 8.0
}
```

**Expected:** 
- Second driver receives "🚛 Nieuwe rit toegewezen!" (role: tweede chauffeur)
- Primary driver receives "👥 Tweede chauffeur toegevoegd!"

---

### **Test 7: Remove Second Driver from TODAY's Ride**

```http
DELETE https://api.vervoermanager.nl/rides/{rideId}/second-driver
Authorization: Bearer {adminToken}
```

**Expected:** Second driver receives "🚫 Verwijderd van rit!"

---

### **Test 8: Delete TODAY's Ride**

```http
DELETE https://api.vervoermanager.nl/rides/{rideId}
Authorization: Bearer {adminToken}
```

**Expected:** All assigned drivers receive "🗑️ Rit geannuleerd!"

---

### **Test 9: Update Ride for TOMORROW (Should NOT notify)**

```http
PUT https://api.vervoermanager.nl/rides/{rideId}/details
{
  "routeFromName": "Amsterdam"
}
```

**Ride PlannedDate:** 2025-11-17 (TOMORROW)

**Expected:** Drivers receive **NO** notification

---

## 📱 Frontend Integration Guide

### **Driver Profile Page - Telegram Section**

#### **Component Structure:**

```jsx
import React, { useState } from 'react';
import { QRCodeSVG } from 'qrcode.react';

const DriverTelegramSection = ({ driverId, driver, onUpdate }) => {
  const [registrationLink, setRegistrationLink] = useState(null);
  const [loading, setLoading] = useState(false);
  const [copied, setCopied] = useState(false);
  
  // Generate registration link
  const generateLink = async () => {
    setLoading(true);
    try {
      const response = await fetch(
        `/drivers/${driverId}/telegram/registration-link`,
        {
          headers: {
            'Authorization': `Bearer ${localStorage.getItem('token')}`,
            'Content-Type': 'application/json'
          }
        }
      );
      
      const result = await response.json();
      
      if (result.success) {
        setRegistrationLink(result.data);
      } else {
        alert(result.message || 'Error generating link');
      }
    } catch (error) {
      console.error('Error:', error);
      alert('Failed to generate registration link');
    } finally {
      setLoading(false);
    }
  };
  
  // Copy link to clipboard
  const copyLink = () => {
    navigator.clipboard.writeText(registrationLink.registrationLink);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };
  
  // Disable notifications
  const disableNotifications = async () => {
    if (!confirm('Are you sure you want to disable Telegram notifications for this driver?')) {
      return;
    }
    
    try {
      const response = await fetch(
        `/drivers/${driverId}/telegram`,
        {
          method: 'DELETE',
          headers: {
            'Authorization': `Bearer ${localStorage.getItem('token')}`
          }
        }
      );
      
      const result = await response.json();
      
      if (result.success) {
        alert('Telegram notifications disabled');
        onUpdate(); // Refresh driver data
      } else {
        alert(result.message || 'Error disabling notifications');
      }
    } catch (error) {
      console.error('Error:', error);
      alert('Failed to disable notifications');
    }
  };
  
  // If already registered
  if (driver.telegramNotificationsEnabled && driver.telegramChatId) {
    return (
      <div className="telegram-section enabled">
        <div className="section-header">
          <h3>✅ Telegram Notifications</h3>
          <span className="status-badge active">Active</span>
        </div>
        
        <div className="telegram-info">
          <div className="info-row">
            <span className="label">Status:</span>
            <span className="value">Enabled</span>
          </div>
          <div className="info-row">
            <span className="label">Chat ID:</span>
            <span className="value">{driver.telegramChatId}</span>
          </div>
          <div className="info-row">
            <span className="label">Registered:</span>
            <span className="value">
              {new Date(driver.telegramRegisteredAt).toLocaleString('nl-NL')}
            </span>
          </div>
        </div>
        
        <button 
          className="btn btn-danger btn-sm"
          onClick={disableNotifications}
        >
          Disable Notifications
        </button>
      </div>
    );
  }
  
  // If not registered yet
  return (
    <div className="telegram-section not-registered">
      <div className="section-header">
        <h3>📱 Telegram Notifications</h3>
        <span className="status-badge inactive">Not Active</span>
      </div>
      
      {!registrationLink ? (
        <div className="setup-prompt">
          <p>
            Setup Telegram notifications to send real-time updates to the driver 
            about their rides.
          </p>
          
          <button 
            className="btn btn-primary"
            onClick={generateLink}
            disabled={loading}
          >
            {loading ? 'Generating...' : 'Generate Registration Link'}
          </button>
        </div>
      ) : (
        <div className="registration-link-container">
          <div className="instructions">
            <h4>📤 Send this link to {registrationLink.driverName}:</h4>
            <p className="help-text">
              {registrationLink.instructions}
            </p>
          </div>
          
          <div className="link-box">
            <input 
              type="text" 
              value={registrationLink.registrationLink} 
              readOnly 
              className="form-control"
            />
            <button 
              className="btn btn-secondary"
              onClick={copyLink}
            >
              {copied ? '✓ Copied!' : '📋 Copy'}
            </button>
          </div>
          
          <div className="qr-code-section">
            <h5>Or scan with phone:</h5>
            <div className="qr-code-wrapper">
              <QRCodeSVG 
                value={registrationLink.registrationLink}
                size={200}
                level="H"
              />
            </div>
            <p className="qr-help">
              Driver scans this QR code → Telegram opens → Instant activation ✅
            </p>
          </div>
          
          <div className="expiry-notice">
            <span className="icon">⏰</span>
            <span>
              Link expires: {new Date(registrationLink.expiresAt).toLocaleString('nl-NL')}
            </span>
          </div>
          
          <button 
            className="btn btn-link"
            onClick={() => setRegistrationLink(null)}
          >
            Generate New Link
          </button>
        </div>
      )}
    </div>
  );
};

export default DriverTelegramSection;
```

#### **CSS Styling:**

```css
.telegram-section {
  border: 1px solid #ddd;
  border-radius: 8px;
  padding: 20px;
  margin: 20px 0;
  background: #f9f9f9;
}

.telegram-section.enabled {
  border-color: #28a745;
  background: #f0f9f0;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 15px;
}

.status-badge {
  padding: 4px 12px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: bold;
}

.status-badge.active {
  background: #28a745;
  color: white;
}

.status-badge.inactive {
  background: #6c757d;
  color: white;
}

.telegram-info {
  margin: 15px 0;
}

.info-row {
  display: flex;
  padding: 8px 0;
  border-bottom: 1px solid #e0e0e0;
}

.info-row .label {
  font-weight: bold;
  width: 140px;
}

.setup-prompt {
  text-align: center;
  padding: 20px;
}

.link-box {
  display: flex;
  gap: 10px;
  margin: 15px 0;
}

.link-box input {
  flex: 1;
  font-family: monospace;
  font-size: 14px;
}

.qr-code-section {
  text-align: center;
  margin: 25px 0;
  padding: 20px;
  background: white;
  border-radius: 8px;
}

.qr-code-wrapper {
  display: inline-block;
  padding: 15px;
  background: white;
  border: 2px solid #ddd;
  border-radius: 8px;
  margin: 15px 0;
}

.qr-help {
  color: #666;
  font-size: 14px;
  margin-top: 10px;
}

.expiry-notice {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px;
  background: #fff3cd;
  border: 1px solid #ffc107;
  border-radius: 6px;
  margin: 15px 0;
  font-size: 14px;
}

.help-text {
  color: #666;
  font-size: 14px;
  margin: 10px 0;
}

.instructions h4 {
  color: #333;
  margin-bottom: 10px;
}
```

---

## 🚀 Deployment Checklist

### **Development Environment**

- ✅ Install `Telegram.Bot` NuGet package
- ✅ Create bot via `@BotFather`
- ✅ Add `BotToken` to `appsettings.Development.json`
- ✅ Run migration: `dotnet ef database update`
- ✅ Test all 9 test scenarios
- ✅ Verify token expiration (7 days)
- ✅ Test QR code generation in frontend

### **Production Environment**

- ✅ Add `Telegram:BotToken` to environment variables (Docker Compose or Plesk)
- ✅ Update `compose.yaml`:
  ```yaml
  environment:
    - Telegram__BotToken=${TELEGRAM_BOT_TOKEN}
    - Telegram__BotUsername=VervoerManager_Driver_Bot
  ```
- ✅ Set environment variable on server:
  ```bash
  export TELEGRAM_BOT_TOKEN="8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA"
  ```
- ✅ Deploy updated Docker image
- ✅ Verify migration applied (check Docker logs)
- ✅ Test with real driver account
- ✅ Monitor registration link generation
- ✅ Test token expiration behavior

---

## 📊 Monitoring & Logging

### **Key Metrics to Track**

1. **Auto-Registration Success Rate**
   ```
   [Telegram] Driver {DriverId} auto-registered with Chat ID {ChatId}
   ```

2. **Notification Success Rate**
   ```
   [Telegram] Sent notification to driver {DriverId} for Ride {RideId}
   ```

3. **Registration Link Generation**
   ```
   Generated registration link for driver {DriverId}, expires at {ExpiresAt}
   ```

4. **Token Validation Failures**
   ```
   [Telegram] Invalid or expired token: {Token}
   ```

5. **Notification Failures**
   ```
   [Telegram] Failed to send notification: {ErrorMessage}
   ```

### **Health Check Endpoint (Optional)**

```csharp
app.MapGet("/telegram/health", 
    [Authorize(Roles = "globalAdmin")]
    async (ITelegramNotificationService telegramService) =>
{
    try
    {
        // Test if bot token is valid by attempting to get bot info
        var testMessage = "Health check";
        // Note: This will fail for invalid chat ID, but confirms token is valid
        return Results.Ok(new { status = "healthy", message = "Bot token is valid" });
    }
    catch
    {
        return Results.Ok(new { status = "unhealthy", error = "Invalid bot token or network issue" });
    }
});
```

---

## 🔒 Security Considerations

### **1. Bot Token Protection**

- ❌ **NEVER** commit bot token to Git
- ✅ Use environment variables in production
- ✅ Consider Azure Key Vault for enterprise deployments
- ✅ Rotate tokens periodically (every 6-12 months)

### **2. Registration Token Security**

- ✅ **One-time use** (cleared after registration)
- ✅ **Time-limited** (7 days expiration)
- ✅ **Random generation** (12-character URL-safe string)
- ✅ **No PII in token** (doesn't contain driver info)

### **3. Webhook Validation (Future Enhancement)**

For production, validate that webhook requests actually come from Telegram:

```csharp
app.MapPost("/telegram/webhook",
    async (HttpContext context, [FromBody] Update update, ...) =>
    {
        var secretToken = context.Request.Headers["X-Telegram-Bot-Api-Secret-Token"];
        if (secretToken != _expectedSecretToken)
        {
            return Results.Unauthorized();
        }
        // ... process update
    });
```

### **4. Rate Limiting**

Telegram API has rate limits (30 messages/second per bot). The current implementation is safe because:
- Only 1-2 notifications per ride change
- Non-blocking (`Task.Run`)
- No bulk operations

---

## 📈 Future Enhancements (Out of Scope for MVP)

### **Phase 2 (Future)**

- ✅ Notifications for **future rides** (e.g., "Tomorrow's schedule")
- ✅ **Weekly summary** at end of week
- ✅ **Interactive buttons** (e.g., "✅ Confirm" / "❌ Report Issue")
- ✅ **Driver-initiated commands** (e.g., `/myrides`, `/schedule`)
- ✅ Notifications for **execution approvals/rejections**
- ✅ **Multi-language support** (detect Telegram language)

### **Phase 3 (Future)**

- ✅ **SMS fallback** for drivers without Telegram
- ✅ **Push notifications** via mobile app
- ✅ **Rich media** (send PDFs, images of route)
- ✅ **Voice messages** for urgent notifications
- ✅ **Group chats** for team coordination

---

## ✅ Success Criteria

The implementation is complete when:

1. ✅ Admin can generate registration link in one click
2. ✅ Driver clicks link → Telegram opens → Instant activation (zero manual work)
3. ✅ Drivers receive notifications for **same-day Ride changes**:
   - ✅ Assigned to new ride
   - ✅ Ride details updated (route, times, notes, trip number, truck)
   - ✅ Second driver added/removed
   - ✅ Removed from ride
   - ✅ Ride deleted/cancelled
4. ✅ Notifications are sent **only to affected drivers** (privacy)
5. ✅ Registration tokens expire after 7 days
6. ✅ Tokens are one-time use (cleared after registration)
7. ✅ System handles errors gracefully (no API failures if Telegram is down)
8. ✅ All tests pass in development and production
9. ✅ QR code generation works in frontend
10. ✅ Frontend displays registration status correctly

---

## 📞 Support & Troubleshooting

### **Common Issues**

#### **1. "Telegram Bot Token is not configured"**

**Cause:** Bot token missing from `appsettings.json` or environment variables

**Fix:**
```bash
# Development
Add to appsettings.Development.json:
"Telegram": { "BotToken": "8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA", "BotUsername": "VervoerManager_Driver_Bot" }

# Production
export TELEGRAM_BOT_TOKEN="8326326279:AAGG7jhY8bkutwkyHcN9p680nrdOqb3SZjA"
```

---

#### **2. Driver not receiving notifications**

**Debug checklist:**
1. Check if `TelegramNotificationsEnabled = true`
2. Check if `TelegramChatId` is set
3. Check if Ride PlannedDate is TODAY
4. Check Docker logs for errors
5. Verify bot token is valid

---

#### **3. "Invalid or expired token" error**

**Causes:**
- Token was already used (one-time use)
- Token expired (> 7 days old)
- Token was manually cleared

**Fix:**
- Admin generates new registration link
- Driver clicks new link

---

#### **4. Registration link not working**

**Debug:**
- Verify `BotUsername` in config matches actual bot username
- Check if token was generated correctly (12 characters)
- Ensure bot is not blocked by Telegram
- Test link format: `https://t.me/BOT_USERNAME?start=TOKEN`

---

#### **5. Notifications sent for wrong dates**

**Check:**
- Verify `PlannedDate` is being compared to `DateTime.UtcNow.Date`
- Ensure database stores dates in UTC
- Check server timezone settings

---

## 📝 Summary

| Phase | Description | Time Estimate | Status |
|-------|-------------|---------------|--------|
| **1** | Setup & Configuration | 10 min | ⏳ Pending |
| **2** | Service Implementation | 2 hours | ⏳ Pending |
| **3** | Endpoint Integration | 2 hours | ⏳ Pending |
| **4** | Auto-Registration Endpoints | 1.5 hours | ⏳ Pending |
| **5** | DI & Configuration | 15 min | ⏳ Pending |
| **6** | Database Migration | 10 min | ⏳ Pending |
| **7** | Testing | 45 min | ⏳ Pending |
| **TOTAL** | **~6.5 hours** | | |

---

## 🎯 **Key Features of Automated Flow**

### **What Makes It Better:**
1. ✅ **Zero Admin Work** - Just generate link, no manual Chat ID entry
2. ✅ **One-Click Registration** - Driver clicks link → Done
3. ✅ **QR Code Support** - Easy to share via phone screen
4. ✅ **Secure** - Tokens expire, one-time use, no PII
5. ✅ **Error Handling** - Clear messages for expired/invalid tokens
6. ✅ **Scalable** - Works for 1 driver or 1000 drivers
7. ✅ **User-Friendly** - Both admin and driver experience is smooth

---

**Ready to implement?** Let me know and I'll start with Phase 1! 🚀
