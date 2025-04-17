namespace TruckManagement.DTOs;

public class SignContractRequest
{
    public string ContractId { get; set; } = null!;
    public string AccessCode { get; set; } = null!;
    public string Signature { get; set; } = null!;
    public IFormFile? PdfFile { get; set; }
}