using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruckManagement.Interfaces;

namespace TruckManagement.Endpoints
{
    public static class TelegramTestEndpoints
    {
        public static void MapTelegramTestEndpoints(this WebApplication app)
        {
            // Test endpoint to manually poll Telegram for updates (for local testing only)
            app.MapPost("/telegram/poll-updates",
                [Authorize(Roles = "globalAdmin")]
                async (ITelegramNotificationService telegramService) =>
                {
                    try
                    {
                        // This will fetch and process any pending messages from Telegram
                        await telegramService.PollAndProcessUpdatesAsync();
                        return Results.Ok(new { success = true, message = "Updates polled and processed successfully" });
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem(
                            detail: ex.Message,
                            statusCode: 500,
                            title: "Error polling Telegram updates"
                        );
                    }
                });
        }
    }
}

