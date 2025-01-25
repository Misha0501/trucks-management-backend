using Microsoft.AspNetCore.Identity;

namespace TruckManagement.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string? Address { get; set; }
        public string? Postcode { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Remark { get; set; }
        // Navigation Properties
        public Driver? Driver { get; set; }
        public ContactPerson? ContactPerson { get; set; }
    }
}