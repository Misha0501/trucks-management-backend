namespace TruckManagement.DTOs
{
    public class UpdateUserRequest
    {
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        // Optional new company ID
        public string? CompanyId { get; set; }

        // Optional list of roles to assign
        public List<string>? Roles { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Postcode { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Remark { get; set; }
    }
}