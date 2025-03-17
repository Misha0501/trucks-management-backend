namespace TruckManagement.Entities
{
    public class HoursOption
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!; 
        // e.g. "Stand Over", "Holiday", "No holiday", "No allowance", ...
        public bool IsActive { get; set; } = false; 

        // Navigation back to PartRides that use this HoursOption
        public ICollection<PartRide> PartRides { get; set; } = new List<PartRide>();
    }
}