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
                HttpContext context,
                ApplicationDbContext db,
                IOptions<TelegramOptions> telegramOptions,
                ITelegramNotificationService telegramService) =>
            {
                try
                {
                    // Read raw JSON from request body
                    using var reader = new StreamReader(context.Request.Body);
                    var json = await reader.ReadToEndAsync();
                    
                    Console.WriteLine($"[Telegram Webhook] Received update: {json}");

                    // Parse JSON manually to extract message text and chat ID
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    // Check if this is a message update with text
                    if (!root.TryGetProperty("message", out var message) ||
                        !message.TryGetProperty("text", out var textElement))
                    {
                        // Not a text message, return 200 OK
                        return Results.Ok();
                    }

                    var messageText = textElement.GetString();
                    if (string.IsNullOrEmpty(messageText) || !messageText.StartsWith("/start"))
                    {
                        // Not a /start command, return 200 OK
                        return Results.Ok();
                    }

                    // Extract chat ID
                    if (!message.TryGetProperty("chat", out var chat) ||
                        !chat.TryGetProperty("id", out var chatIdElement))
                    {
                        return Results.Ok();
                    }

                    var chatId = chatIdElement.GetInt64();

                    // Extract registration token from /start command
                    // Format: /start TOKEN
                    var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                    {
                        // No token - send help message to user
                        await telegramService.SendMessageAsync(
                            chatId, 
                            "ℹ️ <b>Welkom bij VervoerManager Driver Bot!</b>\n\n" +
                            "Om notificaties te ontvangen, moet je een activatielink gebruiken die je van je werkgever hebt ontvangen.\n\n" +
                            "Neem contact op met je werkgever voor een activatielink.");
                        
                        return Results.Ok();
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
                        // Token expired or invalid - notify user
                        await telegramService.SendMessageAsync(
                            chatId, 
                            "❌ <b>Activatielink ongeldig of verlopen</b>\n\n" +
                            "Deze activatielink is niet meer geldig. Activatielinks zijn 24 uur geldig.\n\n" +
                            "Vraag je werkgever om een nieuwe activatielink.");
                        
                        return Results.Ok();
                    }

                    // Register the driver's Telegram chat ID
                    driver.TelegramChatId = chatId;
                    driver.TelegramNotificationsEnabled = true;
                    driver.TelegramRegisteredAt = DateTime.UtcNow;
                    driver.TelegramRegistrationToken = null; // Clear token after successful registration
                    driver.TelegramTokenExpiresAt = null;

                    await db.SaveChangesAsync();

                    // Send success message to driver
                    await telegramService.SendMessageAsync(
                        chatId, 
                        "✅ <b>Registratie geslaagd!</b>\n\n" +
                        "Je ontvangt nu meldingen over je ritten voor vandaag.");

                    Console.WriteLine($"[Telegram Webhook] Driver {driver.Id} registered successfully with Chat ID {chatId}");
                    
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Telegram Webhook] Error: {ex.Message}");
                    Console.WriteLine($"[Telegram Webhook] Stack trace: {ex.StackTrace}");
                    
                    // Always return 200 OK to Telegram, even on errors
                    return Results.Ok();
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

