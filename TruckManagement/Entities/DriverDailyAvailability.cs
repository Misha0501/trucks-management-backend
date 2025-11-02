using System.ComponentModel.DataAnnotations;

namespace TruckManagement.Entities
{
    public class DriverDailyAvailability
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid DriverId { get; set; }
        public Driver Driver { get; set; } = default!;
        
        [Required]
        public DateTime Date { get; set; }
        
        [Required]
        [Range(0, 24)]
        public decimal AvailableHours { get; set; }
        
        [Required]
        public Guid CompanyId { get; set; }
        public Company Company { get; set; } = default!;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

