namespace TruckManagement.DTOs
{
    public class CreateRateRequest
    {
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string ClientId { get; set; } = string.Empty; // As string
    }
}
