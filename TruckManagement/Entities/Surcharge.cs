namespace TruckManagement.Entities
{
    public class Surcharge
    {
        public Guid Id { get; set; }
        public decimal Value { get; set; }

        public Guid ClientId { get; set; }
        public Client Client { get; set; } = default!;

        public Guid CompanyId { get; set; }
        public Company Company { get; set; } = default!;
    }
}