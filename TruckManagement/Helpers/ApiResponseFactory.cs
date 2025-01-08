using TruckManagement.Models;

namespace TruckManagement.Helpers
{
    public static class ApiResponseFactory
    {
        public static IResult Success<T>(T data, int statusCode = StatusCodes.Status200OK)
        {
            var response = new ApiResponse<T>(
                IsSuccess: true,
                StatusCode: statusCode,
                Data: data,
                Errors: null
            );
            return Results.Json(response, statusCode: statusCode);
        }

        public static IResult Error(
            List<string> errors, 
            int statusCode = StatusCodes.Status400BadRequest)
        {
            var response = new ApiResponse<object?>(
                IsSuccess: false,
                StatusCode: statusCode,
                Data: null,
                Errors: errors
            );
            return Results.Json(response, statusCode: statusCode);
        }
        
        // Overload for single string error
        public static IResult Error(
            string errorMessage, 
            int statusCode = StatusCodes.Status400BadRequest)
        {
            return Error(new List<string> { errorMessage }, statusCode);
        }
    }
}
