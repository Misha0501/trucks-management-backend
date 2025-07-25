using System.Text.Json.Serialization;

namespace TruckManagement.Entities
{
    public class Car
    {
        public Guid Id { get; set; }
        public string LicensePlate { get; set; } = default!;
        public int? VehicleYear { get; set; }
        public DateOnly? RegistrationDate { get; set; }
        public string? Remark { get; set; }

        public Guid CompanyId { get; set; }
        public Company Company { get; set; } = default!;
        
        // 1-1 relationship with Driver
        [JsonIgnore] // Prevents circular references in API responses
        public Driver? Driver { get; set; }
        
        // Navigation property for car documents
        public ICollection<CarFile> Files { get; set; } = new List<CarFile>();
    }
}