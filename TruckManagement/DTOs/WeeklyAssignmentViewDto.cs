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
        public string? TripNumber { get; set; }
        public decimal PlannedHours { get; set; } // Total ride hours (for truck)
        public TimeSpan? PlannedStartTime { get; set; }
        public TimeSpan? PlannedEndTime { get; set; }
        public string? RouteFromName { get; set; }
        public string? RouteToName { get; set; }
        public DriverBasicDto? AssignedDriver { get; set; } // Primary driver with hours
        public DriverBasicDto? SecondDriver { get; set; } // Second driver with hours
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
        public decimal PlannedHours { get; set; } // Individual driver hours
    }

    public class CarBasicDto
    {
        public Guid Id { get; set; }
        public string LicensePlate { get; set; } = default!;
    }
}

