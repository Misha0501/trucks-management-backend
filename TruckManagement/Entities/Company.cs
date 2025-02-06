namespace TruckManagement.Entities
{
    public class Company
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        
        public bool IsDeleted { get; set; } // Soft-delete marker
        
        // Proposed items are not yet active until a global admin approves them
        public bool IsApproved { get; set; } = false; 

        // Navigation Properties
        public ICollection<Client> Clients { get; set; } = new List<Client>();
        public ICollection<Driver> Drivers { get; set; } = new List<Driver>();
        public ICollection<ContactPersonClientCompany> ContactPersonClientCompanies { get; set; } = new List<ContactPersonClientCompany>();
    }
}