public class RegisterRequest
{
    public string Email { get; set; } = string.Empty; 
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty; 
    public string LastName { get; set; } = string.Empty;

    public List<string>? Roles { get; set; }   
    // Basic user fields
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Postcode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Remark { get; set; }
    
    // For drivers (only one company):
    public List<string>? CompanyIds { get; set; } // We'll use the first for driver
    
    // For contact persons:
    public List<string>? ClientIds { get; set; }
}