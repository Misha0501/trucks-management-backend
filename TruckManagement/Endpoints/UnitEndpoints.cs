using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.Entities;
using TruckManagement.Models; // Where ApiResponseFactory is assumed to exist
using Microsoft.AspNetCore.Identity;
using TruckManagement.DTOs;
using TruckManagement.Helpers;

namespace TruckManagement.Endpoints;

public static class UnitRoutes
{
    public static void MapUnitEndpoints(this WebApplication app)
    {
        app.MapPost("/units",
            [Authorize(Roles = "globalAdmin")] async (
                [FromBody] CreateUnitRequest request,
                ApplicationDbContext db,
                ClaimsPrincipal currentUser
            ) =>
            {
                try
                {
                    if (request == null || string.IsNullOrWhiteSpace(request.Value))
                    {
                        return ApiResponseFactory.Error(
                            "Unit value is required.",
                            StatusCodes.Status400BadRequest
                        );
                    }

                    var unit = new Unit
                    {
                        Id = Guid.NewGuid(),
                        Value = request.Value
                    };

                    db.Units.Add(unit);
                    await db.SaveChangesAsync();

                    var responseData = new
                    {
                        unit.Id,
                        unit.Value
                    };

                    return ApiResponseFactory.Success(
                        responseData,
                        StatusCodes.Status201Created
                    );
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error creating unit: {ex.Message}");
                    return ApiResponseFactory.Error(
                        "An unexpected error occurred while creating the unit.",
                        StatusCodes.Status500InternalServerError
                    );
                }
            });

        app.MapGet("/units",
            [Authorize(Roles = "globalAdmin")] async (
                ApplicationDbContext db,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 10
            ) =>
            {
                if (pageNumber < 1 || pageSize < 1)
                {
                    return ApiResponseFactory.Error("Invalid pagination parameters.", StatusCodes.Status400BadRequest);
                }

                var query = db.Units.AsQueryable();

                var totalUnits = await query.CountAsync();
                var units = await query
                    .OrderBy(u => u.Value)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new { u.Id, u.Value })
                    .ToListAsync();

                return ApiResponseFactory.Success(new
                {
                    totalUnits,
                    totalPages = (int)Math.Ceiling((double)totalUnits / pageSize),
                    pageNumber,
                    pageSize,
                    units
                }, StatusCodes.Status200OK);
            });

        app.MapGet("/units/{id}",
            [Authorize(Roles = "globalAdmin")] async (string id, ApplicationDbContext db) =>
            {
                if (!Guid.TryParse(id, out Guid unitGuid))
                {
                    return ApiResponseFactory.Error("Invalid unit ID format.", StatusCodes.Status400BadRequest);
                }

                var unit = await db.Units
                    .Where(u => u.Id == unitGuid)
                    .Select(u => new { u.Id, u.Value })
                    .FirstOrDefaultAsync();

                if (unit == null)
                {
                    return ApiResponseFactory.Error("Unit not found.", StatusCodes.Status404NotFound);
                }

                return ApiResponseFactory.Success(unit, StatusCodes.Status200OK);
            });
    }
}