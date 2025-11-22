namespace TruckManagement.DTOs
{
    public class CarDto
    {
        public Guid Id { get; set; }
        public string LicensePlate { get; set; } = default!;
        public int? VehicleYear { get; set; }
        public DateOnly? RegistrationDate { get; set; }
        public DateOnly? LeasingStartDate { get; set; }
        public DateOnly? LeasingEndDate { get; set; }
        public string? Remark { get; set; }
        public Guid CompanyId { get; set; }
        public CompanyDto? Company { get; set; }
        public List<CarFileDto> Files { get; set; } = new List<CarFileDto>();
        
        // Driver assignment information
        public Guid? DriverId { get; set; }
        public string? DriverFirstName { get; set; }
        public string? DriverLastName { get; set; }
        public string? DriverEmail { get; set; }
        
        // Companies that can use this car
        public List<CompanySimpleDto> UsedByCompanies { get; set; } = new List<CompanySimpleDto>();
    }
    
    public class CompanySimpleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
    }

    public class CarFileDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = default!;
        public string OriginalFileName { get; set; } = default!;
        public string ContentType { get; set; } = default!;
        public DateTime UploadedAt { get; set; }
    }
} 