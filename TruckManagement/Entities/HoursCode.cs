namespace TruckManagement.Entities
{
    public class HoursCode
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!; 
        // e.g. "One day ride", "Multi-day trip departure", "Multi-day trip intermediate day", etc.
        public bool IsActive { get; set; } = false; 
        public ICollection<PartRide> PartRides { get; set; } = new List<PartRide>();
    }
}