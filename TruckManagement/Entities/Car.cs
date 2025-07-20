namespace TruckManagement.Entities
{
    public class Car
    {
        public Guid Id { get; set; }
        public string LicensePlate { get; set; } = default!;
        public int? VehicleYear { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public string? Remark { get; set; }

        public Guid CompanyId { get; set; }
        public Company Company { get; set; } = default!;
        
        // Navigation property for car documents
        public ICollection<CarFile> Files { get; set; } = new List<CarFile>();
    }
}