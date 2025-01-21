namespace TruckManagement.Entities
{
    public class Car
    {
        public Guid Id { get; set; }
        public string LicensePlate { get; set; } = default!;
        public string? Remark { get; set; }

        public Guid CompanyId { get; set; }
        public Company Company { get; set; } = default!;
    }
}