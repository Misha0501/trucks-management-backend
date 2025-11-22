namespace TruckManagement.DTOs
{
    public class DriverDto
    {
        public Guid DriverId { get; set; }
        public string AspNetUserId { get; set; } = default!;
        public ApplicationUserDto User { get; set; } = default!;
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        
        // Car assignment information
        public Guid? CarId { get; set; }
        public string? CarLicensePlate { get; set; }
        public int? CarVehicleYear { get; set; }
        public DateOnly? CarRegistrationDate { get; set; }
        public string? CarRemark { get; set; }
        
        // Companies that can use this driver
        public List<CompanySimpleDto> UsedByCompanies { get; set; } = new List<CompanySimpleDto>();
    }
}
