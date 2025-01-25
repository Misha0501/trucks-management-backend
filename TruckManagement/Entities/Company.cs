namespace TruckManagement.Entities
{
    public class Company
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        
        // Navigation Properties
        public ICollection<Client> Clients { get; set; } = new List<Client>();
        public ICollection<Driver> Drivers { get; set; } = new List<Driver>();
        public ICollection<ContactPersonClientCompany> ContactPersonClientCompanies { get; set; } = new List<ContactPersonClientCompany>();
    }
}