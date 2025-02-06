using System.ComponentModel.DataAnnotations.Schema;

namespace TruckManagement.Entities
{
    public class ContactPerson
    {
        public Guid Id { get; set; }
        public bool IsDeleted { get; set; } // Soft-delete marker

        public string AspNetUserId { get; set; } = default!;
        [ForeignKey(nameof(AspNetUserId))]
        public ApplicationUser User { get; set; } = default!;
        // Navigation Properties
        public ICollection<ContactPersonClientCompany> ContactPersonClientCompanies { get; set; } = new List<ContactPersonClientCompany>();
    }
}