using System.ComponentModel.DataAnnotations;

namespace TruckManagement.Entities
{
    public class TruckDailyAvailability
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid TruckId { get; set; }
        public Car Truck { get; set; } = default!;
        
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

