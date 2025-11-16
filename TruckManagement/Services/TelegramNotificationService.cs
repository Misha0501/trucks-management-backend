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
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<TelegramNotificationService> _logger;
        private readonly string _botUsername;

        public TelegramNotificationService(
            IOptions<TelegramOptions> telegramOptions,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<TelegramNotificationService> logger)
        {
            var token = telegramOptions.Value.BotToken;
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("Telegram Bot Token is not configured.");
            }

            _botClient = new TelegramBotClient(token);
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _botUsername = telegramOptions.Value.BotUsername;
        }

        public async Task NotifyDriversOnRideAssignedAsync(Guid rideId, List<Guid> driverIds)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // 5-second timeout protection
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            
            try
            {
                var ride = await db.Rides
                    .Include(r => r.Client)
                    .Include(r => r.Truck)
                    .FirstOrDefaultAsync(r => r.Id == rideId, cts.Token);

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

                // Send notifications in parallel for multiple drivers
                var notificationTasks = new List<Task>();

                foreach (var driverId in driverIds)
                {
                    notificationTasks.Add(Task.Run(async () =>
                    {
                        var driver = await db.Drivers
                            .Include(d => d.User)
                            .FirstOrDefaultAsync(d => d.Id == driverId, cts.Token);

                        if (driver == null || 
                            !driver.TelegramNotificationsEnabled || 
                            !driver.TelegramChatId.HasValue)
                        {
                            _logger.LogDebug("Driver {DriverId} does not have Telegram notifications enabled.", driverId);
                            return;
                        }

                        var assignment = await db.RideDriverAssignments
                            .FirstOrDefaultAsync(rda => rda.RideId == rideId && rda.DriverId == driverId, cts.Token);

                        var message = FormatRideAssignedMessage(ride, assignment?.IsPrimary ?? true);
                        await SendMessageAsync(driver.TelegramChatId.Value, message);

                        _logger.LogInformation(
                            "Sent Telegram notification to driver {DriverId} for assigned Ride {RideId}.", 
                            driverId, 
                            rideId);
                    }, cts.Token));
                }

                await Task.WhenAll(notificationTasks);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Telegram notification timed out for Ride {RideId}.", rideId);
                throw new TimeoutException($"Telegram notification timed out after 5 seconds for Ride {rideId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error sending Telegram notification for Ride assignment {RideId}.", 
                    rideId);
                throw;
            }
        }

        public async Task NotifyDriversOnRideUpdatedAsync(Guid rideId, string changesSummary)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // 5-second timeout protection
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            
            try
            {
                var ride = await db.Rides
                    .Include(r => r.Client)
                    .Include(r => r.Truck)
                    .Include(r => r.DriverAssignments)
                        .ThenInclude(da => da.Driver)
                    .FirstOrDefaultAsync(r => r.Id == rideId, cts.Token);

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

                // Notify all assigned drivers in parallel
                var notificationTasks = ride.DriverAssignments
                    .Where(assignment => 
                        assignment.Driver != null &&
                        assignment.Driver.TelegramNotificationsEnabled &&
                        assignment.Driver.TelegramChatId.HasValue)
                    .Select(async assignment =>
                    {
                        var message = FormatRideUpdatedMessage(ride, changesSummary);
                        await SendMessageAsync(assignment.Driver.TelegramChatId!.Value, message);

                        _logger.LogInformation(
                            "Sent Telegram notification to driver {DriverId} for updated Ride {RideId}.", 
                            assignment.DriverId, 
                            rideId);
                    })
                    .ToList();

                await Task.WhenAll(notificationTasks);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Telegram notification timed out for Ride update {RideId}.", rideId);
                throw new TimeoutException($"Telegram notification timed out after 5 seconds for Ride {rideId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error sending Telegram notification for Ride update {RideId}.", 
                    rideId);
                throw;
            }
        }

        public async Task NotifyDriversOnRideDeletedAsync(Guid rideId, List<Guid> driverIds)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // 5-second timeout protection
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            
            try
            {
                // Notify all drivers in parallel
                var notificationTasks = driverIds.Select(async driverId =>
                {
                    var driver = await db.Drivers.FirstOrDefaultAsync(d => d.Id == driverId, cts.Token);

                    if (driver == null || 
                        !driver.TelegramNotificationsEnabled || 
                        !driver.TelegramChatId.HasValue)
                    {
                        _logger.LogDebug("Driver {DriverId} does not have Telegram notifications enabled.", driverId);
                        return;
                    }

                    var message = FormatRideDeletedMessage();
                    await SendMessageAsync(driver.TelegramChatId.Value, message);

                    _logger.LogInformation(
                        "Sent Telegram notification to driver {DriverId} for deleted Ride {RideId}.", 
                        driverId, 
                        rideId);
                }).ToList();

                await Task.WhenAll(notificationTasks);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Telegram notification timed out for Ride deletion {RideId}.", rideId);
                throw new TimeoutException($"Telegram notification timed out after 5 seconds for Ride {rideId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error sending Telegram notification for Ride deletion {RideId}.", 
                    rideId);
                throw;
            }
        }

        public async Task NotifyDriverOnSecondDriverAddedAsync(Guid rideId, Guid primaryDriverId, string secondDriverName)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // 5-second timeout protection
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            
            try
            {
                var ride = await db.Rides
                    .Include(r => r.Client)
                    .Include(r => r.Truck)
                    .FirstOrDefaultAsync(r => r.Id == rideId, cts.Token);

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

                var driver = await db.Drivers.FirstOrDefaultAsync(d => d.Id == primaryDriverId, cts.Token);

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
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Telegram notification timed out for second driver addition {RideId}.", rideId);
                throw new TimeoutException($"Telegram notification timed out after 5 seconds for Ride {rideId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error sending Telegram notification for second driver addition {RideId}.", 
                    rideId);
                throw;
            }
        }

        public async Task NotifyDriverOnRemovedFromRideAsync(Guid driverId, string rideDetails)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // 5-second timeout protection
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            
            try
            {
                var driver = await db.Drivers.FirstOrDefaultAsync(d => d.Id == driverId, cts.Token);

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
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Telegram notification timed out for driver removal {DriverId}.", driverId);
                throw new TimeoutException($"Telegram notification timed out after 5 seconds for driver {driverId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error sending Telegram notification for driver removal.", 
                    driverId);
                throw;
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

        // ==================== Polling Method (for local testing) ====================

        public async Task PollAndProcessUpdatesAsync()
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            try
            {
                // Get updates from Telegram (this fetches pending messages)
                var updates = await _botClient.GetUpdatesAsync();
                
                _logger.LogInformation("Polled {Count} updates from Telegram", updates.Length);

                foreach (var update in updates)
                {
                    if (update.Message?.Text != null && update.Message.Text.StartsWith("/start"))
                    {
                        var chatId = update.Message.Chat.Id;
                        var messageText = update.Message.Text;

                        // Extract registration token from /start command
                        var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            var registrationToken = parts[1];

                            // Find driver by registration token
                            var driver = await db.Drivers
                                .FirstOrDefaultAsync(d => 
                                    d.TelegramRegistrationToken == registrationToken &&
                                    d.TelegramTokenExpiresAt.HasValue &&
                                    d.TelegramTokenExpiresAt.Value > DateTime.UtcNow);

                            if (driver != null)
                            {
                                // Register the driver's Telegram chat ID
                                driver.TelegramChatId = chatId;
                                driver.TelegramNotificationsEnabled = true;
                                driver.TelegramRegisteredAt = DateTime.UtcNow;
                                driver.TelegramRegistrationToken = null;
                                driver.TelegramTokenExpiresAt = null;

                                await db.SaveChangesAsync();

                                // Send confirmation message
                                await SendMessageAsync(chatId, 
                                    "✅ <b>Registratie geslaagd!</b>\n\n" +
                                    "Je ontvangt nu meldingen over je ritten voor vandaag.");

                                _logger.LogInformation("Driver {DriverId} registered with Telegram Chat ID {ChatId}", 
                                    driver.Id, chatId);
                            }
                        }
                        
                        // Acknowledge the update so Telegram doesn't send it again
                        await _botClient.GetUpdatesAsync(offset: update.Id + 1);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling Telegram updates");
                throw;
            }
        }
    }
}

