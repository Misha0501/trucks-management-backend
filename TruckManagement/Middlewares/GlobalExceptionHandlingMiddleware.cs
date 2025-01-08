using System.Net;
using Microsoft.EntityFrameworkCore;
using Npgsql; // For PostgresException
using TruckManagement.Models;   


public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var statusCode = (int)HttpStatusCode.InternalServerError;
        var errors = new List<string> { "An unexpected error occurred." };

        // Check if this is an EF Core DB update exception
        if (ex is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx)
        {
            // Typically DB constraint issues are a 400, 
            // but you can adjust to 422 (Unprocessable Entity) or another code as needed
            statusCode = StatusCodes.Status400BadRequest;
            errors.Clear();

            // Identify the constraint violation by SQL State
            switch (pgEx.SqlState)
            {
                case "23503":
                    // Foreign key violation
                    errors.Add("A foreign key constraint was violated. " +
                               "Check that referenced records exist or are not in use.");
                    break;

                case "23505":
                    // Unique constraint violation
                    errors.Add("A unique constraint was violated. " +
                               "You might be inserting a duplicate value where it must be unique.");
                    break;

                case "23502":
                    // Not null violation
                    errors.Add("A required column was null, violating a NOT NULL constraint.");
                    break;

                case "23514":
                    // Check constraint violation
                    errors.Add("A check constraint was violated. Make sure your data meets all constraints.");
                    break;

                default:
                    // Some other Postgres-specific error
                    errors.Add($"PostgreSQL Error {pgEx.SqlState}: {pgEx.MessageText}");
                    break;
            }

            // If you want extra detail from pgEx.Detail, pgEx.Hint, or pgEx.ConstraintName:
            if (!string.IsNullOrWhiteSpace(pgEx.Detail))
            {
                errors.Add($"Detail: {pgEx.Detail}");
            }
            if (!string.IsNullOrWhiteSpace(pgEx.ConstraintName))
            {
                errors.Add($"Constraint: {pgEx.ConstraintName}");
            }
        }
        else
        {
            // For all other unhandled exceptions, optionally show the exception message
            // or keep it generic for security reasons
            errors.Add(ex.Message);
        }

        // Build a standardized response object
        var response = new ApiResponse<object>(
            IsSuccess: false,
            StatusCode: statusCode,
            Data: null,
            Errors: errors
        );

        // Write as JSON
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(response);
    }
}
