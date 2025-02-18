namespace TruckManagement.DTOs
{
    public class UpdateRideRequest
    {
        public string? Name { get; set; }
        public string? Remark { get; set; }
        public string? CompanyId { get; set; } // ✅ Now allows company change
    }
}