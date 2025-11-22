namespace TruckManagement.DTOs
{
    public class CompanyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Address { get; set; }
        public string? Postcode { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Remark { get; set; }
        public string? Kvk { get; set; }
        public string? Btw { get; set; }
        public bool IsApproved { get; set; }
        
        // Include associated drivers
        public List<DriverDto> Drivers { get; set; } = new List<DriverDto>();
    }
}