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

        public Guid CompanyId { get; set; }
        public Company Company { get; set; } = default!;
    }
}