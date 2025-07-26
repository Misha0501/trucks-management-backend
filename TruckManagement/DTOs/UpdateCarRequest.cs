namespace TruckManagement.DTOs
{
    public class UpdateCarRequest
    {
        public string? LicensePlate { get; set; }
        public int? VehicleYear { get; set; }
        public DateOnly? RegistrationDate { get; set; }
        public string? Remark { get; set; }
        public string? CompanyId { get; set; }
        public List<UploadFileRequest>? NewUploads { get; set; }
        public List<Guid>? FileIdsToDelete { get; set; }
    }
}