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
        // 1) GET /companies -> List all companies, including their users
        app.MapGet("/companies", async (ApplicationDbContext db) =>
        {
            // Eagerly load Users for each company
            var companies = await db.Companies
                .AsNoTracking()
                .Include(c => c.Users)
                .Select(c => new 
                {
                    c.Id,
                    c.Name,
                    // For each user, select only the fields you want to expose
                    Users = c.Users.Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.FirstName,
                        u.LastName,
                        // If you also want roles, you could join or load them from AspNetUserRoles
                        // or let the front-end call a separate endpoint for user roles
                    }).ToList()
                })
                .ToListAsync();

            return ApiResponseFactory.Success(companies);
        })
        .RequireAuthorization(); // optional

        // 2) GET /companies/{id:guid} -> Single company, including its users
        app.MapGet("/companies/{id:guid}", async (Guid id, ApplicationDbContext db) =>
        {
            // Eagerly load the Users for the specific company
            var company = await db.Companies
                .AsNoTracking()
                .Include(c => c.Users)
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    Users = c.Users.Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.FirstName,
                        u.LastName
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (company == null)
            {
                return ApiResponseFactory.Error("Company not found.", StatusCodes.Status404NotFound);
            }

            return ApiResponseFactory.Success(company);
        })
        .RequireAuthorization(); // optional

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
