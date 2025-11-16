using Telegram.Bot.Types;

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
        
        /// <summary>
        /// Poll Telegram for updates (for local testing without webhooks)
        /// </summary>
        Task PollAndProcessUpdatesAsync();
    }
}

