namespace TruckManagement.DTOs
{
    public class GenerateRidesResponse
    {
        public DateTime WeekStartDate { get; set; }
        public int TotalRidesGenerated { get; set; }
        public List<DayRideResultDto> Days { get; set; } = new List<DayRideResultDto>();
    }

    public class DayRideResultDto
    {
        public DateTime Date { get; set; }
        public List<ClientRideResultDto> Clients { get; set; } = new List<ClientRideResultDto>();
    }

    public class ClientRideResultDto
    {
        public Guid ClientId { get; set; }
        public string ClientName { get; set; } = default!;
        public int RidesGenerated { get; set; }
        public List<Guid> RideIds { get; set; } = new List<Guid>();
    }
}


