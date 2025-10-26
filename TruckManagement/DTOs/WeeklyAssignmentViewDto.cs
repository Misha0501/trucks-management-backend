namespace TruckManagement.DTOs
{
    public class WeeklyAssignmentViewDto
    {
        public string WeekStartDate { get; set; } = default!;
        public List<DayAssignmentDto> Days { get; set; } = new();
    }

    public class DayAssignmentDto
    {
        public string Date { get; set; } = default!;
        public string DayName { get; set; } = default!;
        public List<ClientAssignmentDto> Clients { get; set; } = new();
    }

    public class ClientAssignmentDto
    {
        public Guid ClientId { get; set; }
        public string ClientName { get; set; } = default!;
        public List<RideAssignmentDto> Rides { get; set; } = new();
    }

    public class RideAssignmentDto
    {
        public Guid Id { get; set; }
        public decimal PlannedHours { get; set; }
        public string? RouteFromName { get; set; }
        public string? RouteToName { get; set; }
        public DriverBasicDto? AssignedDriver { get; set; }
        public CarBasicDto? AssignedTruck { get; set; }
        public string? Notes { get; set; }
        public string? CreationMethod { get; set; }
    }

    public class DriverBasicDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string FullName => $"{FirstName} {LastName}";
    }

    public class CarBasicDto
    {
        public Guid Id { get; set; }
        public string LicensePlate { get; set; } = default!;
    }
}

