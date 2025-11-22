namespace TruckManagement.DTOs
{
    public class UpdateDriverRequest
    {
        public string? CompanyId { get; set; }
        public string? CarId { get; set; }
        
        // Companies that can use this driver (null = don't update, empty list = clear all)
        public List<string>? UsedByCompanyIds { get; set; }
        
        // Add more driver-specific fields as necessary
    }
}