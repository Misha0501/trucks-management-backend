namespace TruckManagement.DTOs
{
    public class DailyPlanningDto
    {
        public string Date { get; set; } = default!;
        public string DayName { get; set; } = default!;
        public List<ClientDailyRidesDto> Clients { get; set; } = new();
    }

    public class ClientDailyRidesDto
    {
        public Guid ClientId { get; set; }
        public string ClientName { get; set; } = default!;
        public List<RideAssignmentDto> Rides { get; set; } = new();
    }

    public class AvailableDatesDto
    {
        public List<string> Dates { get; set; } = new();
    }
}

