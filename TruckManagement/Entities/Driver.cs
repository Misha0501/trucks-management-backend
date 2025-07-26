using System.ComponentModel.DataAnnotations.Schema;

namespace TruckManagement.Entities
{
    public class Driver
    {
        public Guid Id { get; set; }

        public Guid? CompanyId { get; set; }
        public Company? Company { get; set; } = default!;
        
        public Guid? CarId { get; set; }
        public Car? Car { get; set; } = default!;
        
        public bool IsDeleted { get; set; } // Soft-delete marker
        public string AspNetUserId { get; set; } = default!;
        [ForeignKey(nameof(AspNetUserId))]
        public ApplicationUser User { get; set; } = default!;
        public DriverCompensationSettings DriverCompensationSettings { get; set; } = default!;
    }
}