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
        
        // Optional: role
        public List<string>? Roles { get; set; }
    }
}