namespace TruckManagement.Entities
{
    /// <summary>
    /// Join table for many-to-many relationship between Driver and Company.
    /// Represents which companies can use a specific driver.
    /// A driver belongs to one company (Driver.CompanyId) but can be used by multiple companies.
    /// </summary>
    public class DriverUsedByCompany
    {
        public Guid Id { get; set; }

        public Guid DriverId { get; set; }
        public Driver Driver { get; set; } = default!;

        public Guid CompanyId { get; set; }
        public Company Company { get; set; } = default!;
    }
}

