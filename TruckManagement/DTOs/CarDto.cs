namespace TruckManagement.DTOs
{
    public class CarDto
    {
        public Guid Id { get; set; }
        public string LicensePlate { get; set; } = default!;
        public int? VehicleYear { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public string? Remark { get; set; }
        public Guid CompanyId { get; set; }
        public CompanyDto? Company { get; set; }
        public List<CarFileDto> Files { get; set; } = new List<CarFileDto>();
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