namespace TruckManagement.DTOs
{
    public class CreateCarRequest
    {
        public string LicensePlate { get; set; } = string.Empty;
        public string? Remark { get; set; }
        public string CompanyId { get; set; } = string.Empty;
    }
}