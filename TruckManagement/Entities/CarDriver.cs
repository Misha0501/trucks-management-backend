namespace TruckManagement.Entities
{
    public class CarDriver
    {
        public Guid Id { get; set; }

        public Guid CarId { get; set; }
        public Car Car { get; set; } = default!;

        public Guid DriverId { get; set; }
        public Driver Driver { get; set; } = default!;
    }
}