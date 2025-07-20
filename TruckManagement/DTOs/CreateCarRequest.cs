namespace TruckManagement.DTOs
{
    public class CreateCarRequest
    {
        public string LicensePlate { get; set; } = string.Empty;
        public int? VehicleYear { get; set; }
        public DateOnly? RegistrationDate { get; set; }
        public string? Remark { get; set; }
        public string CompanyId { get; set; } = string.Empty;
        public List<UploadFileRequest>? NewUploads { get; set; }
    }
}