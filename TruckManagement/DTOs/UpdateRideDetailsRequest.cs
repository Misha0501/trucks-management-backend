using System.ComponentModel.DataAnnotations;

namespace TruckManagement.DTOs
{
    public class UpdateRideDetailsRequest
    {
        [MaxLength(255)]
        public string? RouteFromName { get; set; }

        [MaxLength(255)]
        public string? RouteToName { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public TimeSpan? PlannedStartTime { get; set; }

        public TimeSpan? PlannedEndTime { get; set; }
    }

    public class RideDetailsDto
    {
        public Guid Id { get; set; }
        public string? RouteFromName { get; set; }
        public string? RouteToName { get; set; }
        public string? Notes { get; set; }
        public TimeSpan? PlannedStartTime { get; set; }
        public TimeSpan? PlannedEndTime { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

