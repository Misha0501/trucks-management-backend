namespace TruckManagement.DTOs
{
    public class UpdateRateRequest
    {
        public string Name { get; set; } = string.Empty;
        public decimal? Value { get; set; }
    }
}