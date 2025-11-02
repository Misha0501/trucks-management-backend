namespace TruckManagement.DTOs
{
    public class CapacityTemplateDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public Guid ClientId { get; set; }
        public ClientDto? Client { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MondayTrucks { get; set; }
        public int TuesdayTrucks { get; set; }
        public int WednesdayTrucks { get; set; }
        public int ThursdayTrucks { get; set; }
        public int FridayTrucks { get; set; }
        public int SaturdayTrucks { get; set; }
        public int SundayTrucks { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

