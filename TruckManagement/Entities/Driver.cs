using System.ComponentModel.DataAnnotations.Schema;

namespace TruckManagement.Entities
{
    public class Driver
    {
        public Guid Id { get; set; }

        public Guid? CompanyId { get; set; }
        public Company? Company { get; set; } = default!;

        public string AspNetUserId { get; set; } = default!;
        [ForeignKey(nameof(AspNetUserId))]
        public ApplicationUser User { get; set; } = default!;
    }
}