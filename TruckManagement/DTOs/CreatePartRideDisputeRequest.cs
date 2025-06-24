namespace TruckManagement.DTOs;

public record CreatePartRideDisputeRequest(
    double CorrectionHours,
    string? Comment);
    