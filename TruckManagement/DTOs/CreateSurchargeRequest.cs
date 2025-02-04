namespace TruckManagement.DTOs;

public class CreateSurchargeRequest
{
    public decimal Value { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ClientId { get; set; }
}