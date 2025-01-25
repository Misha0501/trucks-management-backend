namespace TruckManagement.DTOs
{
    public class DriverDto
    {
        public Guid DriverId { get; set; }
        public string AspNetUserId { get; set; } = default!;
        public ApplicationUserDto User { get; set; } = default!;
    }
}
