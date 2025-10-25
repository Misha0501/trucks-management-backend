namespace TruckManagement.DTOs
{
    public class CreateCapacityTemplateRequest
    {
        public string CompanyId { get; set; } = default!;
        public string ClientId { get; set; } = default!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MondayTrucks { get; set; } = 0;
        public int TuesdayTrucks { get; set; } = 0;
        public int WednesdayTrucks { get; set; } = 0;
        public int ThursdayTrucks { get; set; } = 0;
        public int FridayTrucks { get; set; } = 0;
        public int SaturdayTrucks { get; set; } = 0;
        public int SundayTrucks { get; set; } = 0;
        public string? Notes { get; set; }
    }
}

