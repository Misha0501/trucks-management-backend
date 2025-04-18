namespace TruckManagement.DTOs;

public record SendSignMailRequest
{
    public string ContractId { get; init; } = default!;
    public string Email      { get; init; } = default!;
}