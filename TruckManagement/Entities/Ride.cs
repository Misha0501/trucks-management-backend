namespace TruckManagement.Entities
{
    public class Ride
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Remark { get; set; }
        public Guid CompanyId { get; set; }
        public Company Company { get; set; } = default!;
    }
}