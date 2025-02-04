namespace TruckManagement.DTOs;

public class CreateSurchargeRequest
{
    public decimal Value { get; set; }
    public string CompanyId { get; set; }
    public string ClientId { get; set; }
}