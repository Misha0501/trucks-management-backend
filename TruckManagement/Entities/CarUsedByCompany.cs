namespace TruckManagement.Entities
{
    /// <summary>
    /// Join table for many-to-many relationship between Car and Company.
    /// Represents which companies can use a specific car.
    /// A car belongs to one company (Car.CompanyId) but can be used by multiple companies.
    /// </summary>
    public class CarUsedByCompany
    {
        public Guid Id { get; set; }

        public Guid CarId { get; set; }
        public Car Car { get; set; } = default!;

        public Guid CompanyId { get; set; }
        public Company Company { get; set; } = default!;
    }
}

