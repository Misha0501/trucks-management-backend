namespace TruckManagement.DTOs
{
    public class ContactPersonDto
    {
        public Guid ContactPersonId { get; set; }
        public ApplicationUserDto User { get; set; } = default!;
    }
}