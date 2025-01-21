using TruckManagement.Data;
using TruckManagement.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace TruckManagement.Seeding
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            // 1) Resolve the DbContext
            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // 2) Apply any pending migrations
            await dbContext.Database.MigrateAsync();

            // 3) Seed roles
            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var roles = new[] { "employee", "employer", "customer", "customerAdmin", "customerAccountant", "globalAdmin" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new ApplicationRole { Name = role });
                }
            }

            // 4) Seed the default company if it doesn't exist
            var defaultCompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            if (!dbContext.Companies.Any(c => c.Id == defaultCompanyId))
            {
                dbContext.Companies.Add(new Company
                {
                    Id = defaultCompanyId,
                    Name = "DefaultCompany"
                });
                await dbContext.SaveChangesAsync();
            }

            // 5) Seed additional companies
            var companiesToSeed = new List<Company>
            {
                new Company
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "SecondCompany"
                },
                new Company
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Name = "ThirdCompany"
                },
                new Company
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Name = "FourthCompany"
                }
            };

            foreach (var comp in companiesToSeed)
            {
                if (!dbContext.Companies.Any(c => c.Id == comp.Id))
                {
                    dbContext.Companies.Add(comp);
                }
            }

            await dbContext.SaveChangesAsync();

            // 6) Seed sample users
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Example #1: A globalAdmin user
            const string adminEmail = "admin@admin.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "User",
                };

                // Create user with a sample password
                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    // Assign the globalAdmin role
                    await userManager.AddToRoleAsync(adminUser, "globalAdmin");
                }
            }

            // Example #2: A customer user
            const string customerEmail = "customer@example.com";
            var customerUser = await userManager.FindByEmailAsync(customerEmail);
            if (customerUser == null)
            {
                customerUser = new ApplicationUser
                {
                    UserName = customerEmail,
                    Email = customerEmail,
                    FirstName = "John",
                    LastName = "Customer",
                    // Suppose we link this user to "SecondCompany" for demonstration
                };

                var result = await userManager.CreateAsync(customerUser, "Customer@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(customerUser, "customer");
                }
            }

            // Example #3: An employee user
            const string employeeEmail = "employee@example.com";
            var employeeUser = await userManager.FindByEmailAsync(employeeEmail);
            if (employeeUser == null)
            {
                employeeUser = new ApplicationUser
                {
                    UserName = employeeEmail,
                    Email = employeeEmail,
                    FirstName = "Emily",
                    LastName = "Employee",
                    // Maybe link to "ThirdCompany"
                };

                var result = await userManager.CreateAsync(employeeUser, "Employee@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(employeeUser, "employee");
                }
            }

            // You can add more users with different roles as needed
        }
    }
}
