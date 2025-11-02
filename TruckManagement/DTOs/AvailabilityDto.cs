namespace TruckManagement.DTOs
{
    // Response DTOs
    public class WeeklyAvailabilityDto
    {
        public string WeekStartDate { get; set; } = default!;
        public List<DriverAvailabilityDto> Drivers { get; set; } = new();
        public List<TruckAvailabilityDto> Trucks { get; set; } = new();
    }

    public class DriverAvailabilityDto
    {
        public Guid DriverId { get; set; }
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string FullName => $"{FirstName} {LastName}";
        public Dictionary<string, DayAvailabilityDto> Availability { get; set; } = new();
    }

    public class TruckAvailabilityDto
    {
        public Guid TruckId { get; set; }
        public string LicensePlate { get; set; } = default!;
        public Dictionary<string, DayAvailabilityDto> Availability { get; set; } = new();
    }

    public class DayAvailabilityDto
    {
        public decimal Hours { get; set; }
        public bool IsCustom { get; set; }
    }

    // Request DTOs
    public class BulkAvailabilityRequest
    {
        public Dictionary<string, decimal> Availability { get; set; } = new();
    }

    // Response for bulk updates
    public class BulkAvailabilityResponse
    {
        public Guid ResourceId { get; set; }
        public List<UpdatedDateDto> UpdatedDates { get; set; } = new();
    }

    public class UpdatedDateDto
    {
        public string Date { get; set; } = default!;
        public decimal Hours { get; set; }
    }
}

