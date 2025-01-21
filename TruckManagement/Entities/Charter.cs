namespace TruckManagement.Entities
{
    public class Charter
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        
        public Guid ClientId { get; set; }
        public Client Client { get; set; } = default!;

        public Guid CompanyId { get; set; }
        public Company Company { get; set; } = default!;

        public string? Remark { get; set; }
    }
}