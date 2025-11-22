using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TruckManagement.Entities
{
    public class Driver
    {
        public Guid Id { get; set; }

        public Guid? CompanyId { get; set; }
        public Company? Company { get; set; } = default!;
        
        public Guid? CarId { get; set; }
        public Car? Car { get; set; } = default!;
        
        public bool IsDeleted { get; set; } // Soft-delete marker
        public string AspNetUserId { get; set; } = default!;
        [ForeignKey(nameof(AspNetUserId))]
        public ApplicationUser User { get; set; } = default!;
        public DriverCompensationSettings DriverCompensationSettings { get; set; } = default!;
        
        public ICollection<DriverFile> Files { get; set; } = new List<DriverFile>();
        
        // Many-to-many: Companies that can use this driver
        [JsonIgnore] // Prevents circular references in API responses
        public ICollection<DriverUsedByCompany> UsedByCompanies { get; set; } = new List<DriverUsedByCompany>();

        // Telegram notification fields
        /// <summary>
        /// Telegram Chat ID for sending notifications to this driver.
        /// This is a unique identifier assigned by Telegram when the driver registers with the bot.
        /// </summary>
        public long? TelegramChatId { get; set; }

        /// <summary>
        /// Indicates whether Telegram notifications are enabled for this driver.
        /// </summary>
        public bool TelegramNotificationsEnabled { get; set; } = false;

        /// <summary>
        /// Timestamp when the driver registered with the Telegram bot.
        /// </summary>
        public DateTime? TelegramRegisteredAt { get; set; }

        /// <summary>
        /// One-time registration token for automatic Telegram bot registration.
        /// This token is generated when an admin creates a registration link.
        /// </summary>
        public string? TelegramRegistrationToken { get; set; }

        /// <summary>
        /// Expiration timestamp for the registration token (typically 24 hours after generation).
        /// </summary>
        public DateTime? TelegramTokenExpiresAt { get; set; }
    }
}