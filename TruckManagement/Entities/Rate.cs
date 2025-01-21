namespace TruckManagement.Entities
{
    public class Rate
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public decimal Value { get; set; }

        public Guid ClientId { get; set; }
        public Client Client { get; set; } = default!;

        public Guid CompanyId { get; set; }
        public Company Company { get; set; } = default!;
    }
}