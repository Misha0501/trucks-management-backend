namespace TruckManagement.DTOs
{
    public class UpdateContactPersonRequest
    {
        public List<string>? CompanyIds { get; set; }
        public List<string>? ClientIds { get; set; }
        // Add more contact person–specific fields as necessary
    }
}