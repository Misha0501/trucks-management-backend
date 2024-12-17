using Microsoft.AspNetCore.Identity;

namespace TruckManagement.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public Guid CompanyId { get; set; }
        public Company Company { get; set; } = default!;
    }
}