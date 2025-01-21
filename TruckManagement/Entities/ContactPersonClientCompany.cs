namespace TruckManagement.Entities
{
    public class ContactPersonClientCompany
    {
        public Guid Id { get; set; }
        public Guid ContactPersonId { get; set; }
        public ContactPerson ContactPerson { get; set; }
    
        public Guid? CompanyId { get; set; } 
        public Company? Company { get; set; }

        public Guid? ClientId { get; set; }
        public Client? Client { get; set; }
    }
}