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
        
        public decimal PlannedHours { get; set; } = 8.0m;
        
        public string? RouteFromName { get; set; }
        
        public string? RouteToName { get; set; }
        
        public string? Notes { get; set; }
        
        public string? CreationMethod { get; set; } // e.g., "TEMPLATE_GENERATED", "MANUAL"
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public ICollection<PartRide> PartRides { get; set; } = new List<PartRide>();
    }
}