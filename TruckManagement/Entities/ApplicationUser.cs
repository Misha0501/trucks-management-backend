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
        public bool IsDeleted { get; set; } // Soft-delete marker
        
        // Proposed items are not yet active until a global admin approves them
        public bool IsApproved { get; set; } = false; 

        // Navigation Properties
        public Driver? Driver { get; set; }
        public ContactPerson? ContactPerson { get; set; }
    }
}