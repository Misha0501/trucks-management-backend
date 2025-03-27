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
        public DbSet<PartRideApproval> PartRideApprovals { get; set; }
        public DbSet<PartRideComment> PartRideComments { get; set; }
        public DbSet<HoursOption> HoursOptions { get; set; } = default!;
        public DbSet<HoursCode> HoursCodes { get; set; } = default!;
        public DbSet<DriverCompensationSettings> DriverCompensationSettings { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Apply global filters
            builder.Entity<Company>().HasQueryFilter(c => !c.IsDeleted && c.IsApproved);
            builder.Entity<Client>().HasQueryFilter(c => !c.IsDeleted && c.IsApproved);
            builder.Entity<ApplicationUser>().HasQueryFilter(u => !u.IsDeleted && u.IsApproved);
            builder.Entity<Driver>().HasQueryFilter(d => !d.IsDeleted);
            builder.Entity<ContactPerson>().HasQueryFilter(cp => !cp.IsDeleted);

            // Apply matching filters to dependent entities
            builder.Entity<Car>().HasQueryFilter(c => !c.Company.IsDeleted);
            builder.Entity<Charter>().HasQueryFilter(ch => !ch.Client.IsDeleted && !ch.Company.IsDeleted);
            builder.Entity<Rate>().HasQueryFilter(r => !r.Client.IsDeleted && !r.Company.IsDeleted);
            builder.Entity<Surcharge>().HasQueryFilter(s => !s.Client.IsDeleted && !s.Company.IsDeleted);

            builder.Entity<Company>()
                .ToTable("Companies");

            // Company ↔ Client (One-to-Many)
            builder.Entity<Client>()
                .HasOne(c => c.Company)
                .WithMany(cmp => cmp.Clients)
                .HasForeignKey(c => c.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Company ↔ Driver (One-to-Many)
            builder.Entity<Driver>()
                .HasOne(d => d.Company)
                .WithMany(cmp => cmp.Drivers)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // ContactPerson ↔ ContactPersonClientCompany (One-to-Many)
            builder.Entity<ContactPersonClientCompany>()
                .HasOne(cpc => cpc.ContactPerson)
                .WithMany(cp => cp.ContactPersonClientCompanies)
                .HasForeignKey(cpc => cpc.ContactPersonId)
                .OnDelete(DeleteBehavior.Cascade);

            // ContactPersonClientCompany ↔ Company (Many-to-One)
            builder.Entity<ContactPersonClientCompany>()
                .HasOne(cpc => cpc.Company)
                .WithMany(cmp => cmp.ContactPersonClientCompanies)
                .HasForeignKey(cpc => cpc.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            // ContactPersonClientCompany ↔ Client (Many-to-One)
            builder.Entity<ContactPersonClientCompany>()
                .HasOne(cpc => cpc.Client)
                .WithMany(c => c.ContactPersonClientCompanies)
                .HasForeignKey(cpc => cpc.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            // ApplicationUser ↔ Driver (One-to-One)
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Driver)
                .WithOne(d => d.User)
                .HasForeignKey<Driver>(d => d.AspNetUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ApplicationUser ↔ ContactPerson (One-to-One)
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.ContactPerson)
                .WithOne(cp => cp.User)
                .HasForeignKey<ContactPerson>(cp => cp.AspNetUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Approvals
            builder.Entity<PartRideApproval>(entity =>
            {
                // one-to-many from PartRide to Approvals
                entity.HasOne(pa => pa.PartRide)
                    .WithMany(pr => pr.Approvals)
                    .HasForeignKey(pa => pa.PartRideId)
                    .OnDelete(DeleteBehavior.Cascade);

                // link PartRideApproval -> ApplicationRole
                entity.HasOne(pa => pa.Role)
                    .WithMany() // no navigation in ApplicationRole
                    .HasForeignKey(pa => pa.RoleId)
                    .IsRequired();

                // Index perhaps
                entity.HasIndex(pa => new { pa.PartRideId, pa.RoleId });
            });

            // Comments
            builder.Entity<PartRideComment>(entity =>
            {
                // one-to-many from PartRide to Comments
                entity.HasOne(pc => pc.PartRide)
                    .WithMany(pr => pr.Comments)
                    .HasForeignKey(pc => pc.PartRideId)
                    .OnDelete(DeleteBehavior.Cascade);

                // If you link AuthorRole
                entity.HasOne(pc => pc.AuthorRole)
                    .WithMany()
                    .HasForeignKey(pc => pc.AuthorRoleId)
                    .IsRequired(false);
            });
            builder.Entity<PartRide>()
                .HasOne(pr => pr.HoursOption)
                .WithMany(ho => ho.PartRides)
                .HasForeignKey(pr => pr.HoursOptionId)
                .OnDelete(DeleteBehavior.Restrict);

            // PartRide -> HoursCode
            builder.Entity<PartRide>()
                .HasOne(pr => pr.HoursCode)
                .WithMany(hc => hc.PartRides)
                .HasForeignKey(pr => pr.HoursCodeId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Driver ↔ DriverCompensationSettings (1-to-1)
            builder.Entity<Driver>()
                .HasOne(d => d.DriverCompensationSettings)
                .WithOne(dcs => dcs.Driver)
                .HasForeignKey<DriverCompensationSettings>(dcs => dcs.DriverId)
                .OnDelete(DeleteBehavior.Cascade); // Or Restrict if needed

            builder.Entity<DriverCompensationSettings>(entity =>
            {
                entity.Property(x => x.DriverRatePerHour).HasColumnType("decimal(10,2)");
                entity.Property(x => x.NightAllowanceRate).HasColumnType("decimal(5,4)");
                entity.Property(x => x.KilometerAllowance).HasColumnType("decimal(5,3)");
                entity.Property(x => x.HourlyRate).HasColumnType("decimal(10,2)");
                entity.Property(x => x.Salary4Weeks).HasColumnType("decimal(10,2)");
                entity.Property(x => x.WeeklySalary).HasColumnType("decimal(10,2)");
            });
        }
    }
}