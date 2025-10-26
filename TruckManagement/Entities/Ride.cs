using System.ComponentModel.DataAnnotations;

namespace TruckManagement.Entities
{
    public class Ride
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        // Legacy fields (keeping for backward compatibility)
        public string? Name { get; set; }
        public string? Remark { get; set; }
        
        [Required]
        public Guid CompanyId { get; set; }
        public Company Company { get; set; } = default!;
        
        // New planning fields
        public Guid? ClientId { get; set; }
        public Client? Client { get; set; }
        
        public DateTime? PlannedDate { get; set; }
        
        public decimal TotalPlannedHours { get; set; } = 8.0m; // For truck scheduling
        
        public TimeSpan? PlannedStartTime { get; set; } // Time of day (e.g., 08:00:00)
        
        public TimeSpan? PlannedEndTime { get; set; } // Time of day (e.g., 17:00:00)
        
        public Guid? TruckId { get; set; }
        public Car? Truck { get; set; }
        
        public string? RouteFromName { get; set; }
        
        public string? RouteToName { get; set; }
        
        public string? Notes { get; set; }
        
        public string? CreationMethod { get; set; } // e.g., "TEMPLATE_GENERATED", "MANUAL"
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public ICollection<RideDriverAssignment> DriverAssignments { get; set; } = new List<RideDriverAssignment>();
        public ICollection<PartRide> PartRides { get; set; } = new List<PartRide>();
    }
}