namespace TruckManagement.Models
{
    public record ApiResponse<T>(
        bool IsSuccess,
        int StatusCode,
        T? Data,
        List<string>? Errors
    );
}
