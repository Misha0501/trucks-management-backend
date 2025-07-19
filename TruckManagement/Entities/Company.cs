namespace TruckManagement.Entities
{
    public class Company
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
        
        public bool IsDeleted { get; set; } // Soft-delete marker
        
        // Proposed items are not yet active until a global admin approves them
        public bool IsApproved { get; set; } = false; 

        // Navigation Properties
        public ICollection<Client> Clients { get; set; } = new List<Client>();
        public ICollection<Driver> Drivers { get; set; } = new List<Driver>();
        public ICollection<ContactPersonClientCompany> ContactPersonClientCompanies { get; set; } = new List<ContactPersonClientCompany>();
    }
}