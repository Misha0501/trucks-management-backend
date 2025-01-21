using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Entities;

namespace TruckManagement.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<ContactPerson> ContactPersons { get; set; }
        public DbSet<ContactPersonClientCompany> ContactPersonClientCompanies { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Rate> Rates { get; set; }
        public DbSet<Surcharge> Surcharges { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<CarDriver> CarDrivers { get; set; }
        public DbSet<Car> Cars { get; set; }
        public DbSet<Ride> Rides { get; set; }
        public DbSet<Charter> Charters { get; set; }
        public DbSet<PartRide> PartRides { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Company>()
                .ToTable("Companies");
        }
    }
}