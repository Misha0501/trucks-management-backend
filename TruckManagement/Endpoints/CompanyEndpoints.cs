using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.Entities;
using TruckManagement.Helpers;

namespace TruckManagement.Endpoints;

public static class CompanyEndpoints
{
    public static WebApplication MapCompanyEndpoints(this WebApplication app)
    {
        // 1) GET /companies -> List all (any authenticated user can read, or you can remove RequireAuthorization if you want public)
        app.MapGet("/companies", async (ApplicationDbContext db) =>
        {
            var companies = await db.Companies.AsNoTracking().ToListAsync();
            return ApiResponseFactory.Success(companies);
        });

        // 2) GET /companies/{id:guid} -> Get single (any authenticated user)
        app.MapGet("/companies/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            var company = await db.Companies.FindAsync(id);
            if (company == null)
            {
                return ApiResponseFactory.Error("Company not found.", StatusCodes.Status404NotFound);
            }

            return ApiResponseFactory.Success(company);
        });
        
        // 3) POST /companies -> Create a new company (Require globalAdmin)
        app.MapPost("/companies", async (
            [FromBody] Company newCompany,
            ApplicationDbContext db) =>
        {
            if (newCompany.Id == Guid.Empty)
                newCompany.Id = Guid.NewGuid();

            db.Companies.Add(newCompany);
            await db.SaveChangesAsync();

            return ApiResponseFactory.Success(newCompany, StatusCodes.Status201Created);
        })
        .RequireAuthorization("GlobalAdminOnly"); // <--- policy name

        // 4) PUT /companies/{id:guid} -> Update (Require globalAdmin)
        app.MapPut("/companies/{id:guid}", async (
            Guid id,
            [FromBody] Company updatedCompany,
            ApplicationDbContext db) =>
        {
            var existing = await db.Companies.FindAsync(id);
            if (existing == null)
            {
                return ApiResponseFactory.Error("Company not found.", StatusCodes.Status404NotFound);
            }

            existing.Name = updatedCompany.Name;
            await db.SaveChangesAsync();
            return ApiResponseFactory.Success(existing);
        })
        .RequireAuthorization("GlobalAdminOnly"); // <--- policy name

        // 5) DELETE /companies/{id:guid} -> Delete (Require globalAdmin)
        app.MapDelete("/companies/{id:guid}", async (
            Guid id,
            ApplicationDbContext db) =>
        {
            var existing = await db.Companies.FindAsync(id);
            if (existing == null)
            {
                return ApiResponseFactory.Error("Company not found.", StatusCodes.Status404NotFound);
            }

            db.Companies.Remove(existing);
            await db.SaveChangesAsync();

            return ApiResponseFactory.Success("Company deleted successfully.", StatusCodes.Status200OK);
        })
        .RequireAuthorization("GlobalAdminOnly"); // <--- policy name

        return app;
    }
}
