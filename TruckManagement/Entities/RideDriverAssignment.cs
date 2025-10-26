using System.ComponentModel.DataAnnotations;

namespace TruckManagement.Entities
{
    public class RideDriverAssignment
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid RideId { get; set; }
        public Ride Ride { get; set; } = default!;

        [Required]
        public Guid DriverId { get; set; }
        public Driver Driver { get; set; } = default!;

        public decimal PlannedHours { get; set; }

        public bool IsPrimary { get; set; } = true;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}

