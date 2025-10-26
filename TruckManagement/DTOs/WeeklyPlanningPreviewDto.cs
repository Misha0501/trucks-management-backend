namespace TruckManagement.DTOs
{
    public class WeeklyPlanningPreviewDto
    {
        public string WeekStartDate { get; set; } = default!;
        public List<DayPreviewDto> Days { get; set; } = new();
    }

    public class DayPreviewDto
    {
        public string Date { get; set; } = default!;
        public string DayName { get; set; } = default!;
        public List<ClientDayPreviewDto> Clients { get; set; } = new();
    }

    public class ClientDayPreviewDto
    {
        public Guid ClientId { get; set; }
        public string ClientName { get; set; } = default!;
        public int TrucksNeeded { get; set; }
        public List<Guid> SourceTemplates { get; set; } = new();
    }
}


