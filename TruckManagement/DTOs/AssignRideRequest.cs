namespace TruckManagement.DTOs
{
    public class AssignRideRequest
    {
        public Guid? DriverId { get; set; }
        public decimal? DriverPlannedHours { get; set; }
        public Guid? TruckId { get; set; }
        public decimal TotalPlannedHours { get; set; }
    }

    public class AddSecondDriverRequest
    {
        public Guid DriverId { get; set; }
        public decimal PlannedHours { get; set; }
    }

    public class UpdateRideHoursRequest
    {
        public decimal TotalPlannedHours { get; set; }
        public decimal? PrimaryDriverHours { get; set; }
        public decimal? SecondDriverHours { get; set; }
    }
}

