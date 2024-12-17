namespace TruckManagement.Entities
{
    public class Company
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    }
}