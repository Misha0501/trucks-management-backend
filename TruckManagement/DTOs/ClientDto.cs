namespace TruckManagement.DTOs
{
    public class ClientDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Tav { get; set; }
        public string? Address { get; set; }
        public string? Postcode { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Remark { get; set; }
        
        public bool IsApproved { get; set; } 

        // Include Company details
        public CompanyDto Company { get; set; } = default!;
    }
}