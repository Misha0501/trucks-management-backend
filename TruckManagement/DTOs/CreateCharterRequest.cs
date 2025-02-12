namespace TruckManagement.DTOs
{
    public class CreateCharterRequest
    {
        public string Name { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string? Remark { get; set; }
    }
}