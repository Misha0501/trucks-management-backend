namespace TruckManagement.Entities
{
    public class Client
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

        public bool IsDeleted { get; set; } // Soft-delete marker
        
        // Proposed items are not yet active until a global admin approves them
        public bool IsApproved { get; set; } = false; 

        public Guid CompanyId { get; set; }
        public Company Company { get; set; } = default!;
        // Navigation Properties
        public ICollection<ContactPersonClientCompany> ContactPersonClientCompanies { get; set; } = new List<ContactPersonClientCompany>();
        public ICollection<PartRide> PartRides { get; set; } = new List<PartRide>();
    }
}