namespace TruckManagement.DTOs
{
    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty; 
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty; 
        public string LastName { get; set; } = string.Empty;
        public string CompanyId { get; set; } = string.Empty;
        public List<string>? Roles { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Postcode { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Remark { get; set; }
    }
}