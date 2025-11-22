namespace TruckManagement.DTOs
{
    public class UpdateCarRequest
    {
        public string? LicensePlate { get; set; }
        public int? VehicleYear { get; set; }
        public DateOnly? RegistrationDate { get; set; }
        public DateOnly? LeasingStartDate { get; set; }
        public DateOnly? LeasingEndDate { get; set; }
        public string? Remark { get; set; }
        public string? CompanyId { get; set; }
        public List<UploadFileRequest>? NewUploads { get; set; }
        public List<Guid>? FileIdsToDelete { get; set; }
        
        // Companies that can use this car (null = don't update, empty list = clear all)
        public List<string>? UsedByCompanyIds { get; set; }
    }
}