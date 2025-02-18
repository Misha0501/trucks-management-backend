namespace TruckManagement.DTOs
{
    public class CreateRideRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Remark { get; set; }
        public string CompanyId { get; set; } = string.Empty;
    }
}