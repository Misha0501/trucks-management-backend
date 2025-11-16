using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
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
            // POST /telegram/webhook - Telegram bot webhook endpoint (for automatic registration)
            app.MapPost("/telegram/webhook", async (
                [FromBody] Update update,
                ApplicationDbContext db,
                IOptions<TelegramOptions> telegramOptions) =>
            {
                try
                {
                    // Only handle text messages with /start command
                    if (update.Message?.Text == null || !update.Message.Text.StartsWith("/start"))
                    {
                        return Results.Ok("Ignored");
                    }

                    var chatId = update.Message.Chat.Id;
                    var messageText = update.Message.Text;

                    // Extract registration token from /start command
                    // Format: /start TOKEN
                    var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                    {
                        return Results.Ok("No token provided");
                    }

                    var registrationToken = parts[1];

                    // Find driver by registration token
                    var driver = await db.Drivers
                        .FirstOrDefaultAsync(d => 
                            d.TelegramRegistrationToken == registrationToken &&
                            d.TelegramTokenExpiresAt.HasValue &&
                            d.TelegramTokenExpiresAt.Value > DateTime.UtcNow);

                    if (driver == null)
                    {
                        // Token expired or invalid
                        return Results.Ok("Invalid or expired token");
                    }

                    // Register the driver's Telegram chat ID
                    driver.TelegramChatId = chatId;
                    driver.TelegramNotificationsEnabled = true;
                    driver.TelegramRegisteredAt = DateTime.UtcNow;
                    driver.TelegramRegistrationToken = null; // Clear token after successful registration
                    driver.TelegramTokenExpiresAt = null;

                    await db.SaveChangesAsync();

                    return Results.Ok("Driver registered successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Telegram Webhook] Error: {ex.Message}");
                    return Results.Ok("Error processing webhook");
                }
            }).AllowAnonymous(); // Webhook needs to be accessible without auth

            // GET /drivers/{id}/telegram/registration-link - Generate registration link for driver
            app.MapGet("/drivers/{id}/telegram/registration-link",
                [Authorize(Roles = "globalAdmin, customerAdmin")]
                async (
                    Guid id,
                    ApplicationDbContext db,
                    IOptions<TelegramOptions> telegramOptions,
                    ClaimsPrincipal currentUser) =>
                {
                    try
                    {
                        var driver = await db.Drivers
                            .Include(d => d.User)
                            .FirstOrDefaultAsync(d => d.Id == id);

                        if (driver == null)
                        {
                            return ApiResponseFactory.Error("Driver not found.", StatusCodes.Status404NotFound);
                        }

                        // Generate a unique registration token (24-hour validity)
                        var registrationToken = Guid.NewGuid().ToString("N"); // 32 hex chars
                        driver.TelegramRegistrationToken = registrationToken;
                        driver.TelegramTokenExpiresAt = DateTime.UtcNow.AddHours(24);

                        await db.SaveChangesAsync();

                        var botUsername = telegramOptions.Value.BotUsername;
                        var registrationUrl = $"https://t.me/{botUsername}?start={registrationToken}";

                        var response = new
                        {
                            DriverId = driver.Id,
                            DriverName = $"{driver.User?.FirstName} {driver.User?.LastName}".Trim(),
                            RegistrationUrl = registrationUrl,
                            ExpiresAt = driver.TelegramTokenExpiresAt,
                            Instructions = "De chauffeur moet op deze link klikken om Telegram notificaties te activeren.",
                            AlreadyRegistered = driver.TelegramChatId.HasValue
                        };

                        return ApiResponseFactory.Success(response);
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error(
                            $"Error generating registration link: {ex.Message}",
                            StatusCodes.Status500InternalServerError);
                    }
                });

            // DELETE /drivers/{id}/telegram - Disable Telegram notifications for driver
            app.MapDelete("/drivers/{id}/telegram",
                [Authorize(Roles = "globalAdmin, customerAdmin")]
                async (
                    Guid id,
                    ApplicationDbContext db,
                    ClaimsPrincipal currentUser) =>
                {
                    try
                    {
                        var driver = await db.Drivers.FirstOrDefaultAsync(d => d.Id == id);

                        if (driver == null)
                        {
                            return ApiResponseFactory.Error("Driver not found.", StatusCodes.Status404NotFound);
                        }

                        // Clear Telegram registration data
                        driver.TelegramChatId = null;
                        driver.TelegramNotificationsEnabled = false;
                        driver.TelegramRegisteredAt = null;
                        driver.TelegramRegistrationToken = null;
                        driver.TelegramTokenExpiresAt = null;

                        await db.SaveChangesAsync();

                        return ApiResponseFactory.Success("Telegram notifications disabled successfully.");
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error(
                            $"Error disabling Telegram notifications: {ex.Message}",
                            StatusCodes.Status500InternalServerError);
                    }
                });

            // POST /telegram/test - Send test message to driver (for testing only)
            app.MapPost("/telegram/test",
                [Authorize(Roles = "globalAdmin")]
                async (
                    [FromBody] SendTelegramTestRequest request,
                    ApplicationDbContext db,
                    ITelegramNotificationService telegramService) =>
                {
                    try
                    {
                        var driver = await db.Drivers.FirstOrDefaultAsync(d => d.Id == request.DriverId);

                        if (driver == null)
                        {
                            return ApiResponseFactory.Error("Driver not found.", StatusCodes.Status404NotFound);
                        }

                        if (!driver.TelegramChatId.HasValue)
                        {
                            return ApiResponseFactory.Error(
                                "Driver does not have Telegram enabled.",
                                StatusCodes.Status400BadRequest);
                        }

                        var testMessage = string.IsNullOrWhiteSpace(request.Message)
                            ? "🧪 <b>Test bericht</b>\n\nDit is een testbericht van VervoerManager."
                            : request.Message;

                        await telegramService.SendMessageAsync(driver.TelegramChatId.Value, testMessage);

                        return ApiResponseFactory.Success(new
                        {
                            Message = "Test message sent successfully.",
                            SentTo = driver.TelegramChatId.Value,
                            MessageContent = testMessage
                        });
                    }
                    catch (Exception ex)
                    {
                        return ApiResponseFactory.Error(
                            $"Error sending test message: {ex.Message}",
                            StatusCodes.Status500InternalServerError);
                    }
                });
        }
    }

    // DTO for test message request
    public class SendTelegramTestRequest
    {
        public Guid DriverId { get; set; }
        public string? Message { get; set; }
    }
}

