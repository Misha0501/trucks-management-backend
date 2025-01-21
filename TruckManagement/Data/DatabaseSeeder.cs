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
            var roles = new[] { "driver", "employer", "customer", "customerAdmin", "customerAccountant", "globalAdmin" };
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
                };

                var result = await userManager.CreateAsync(employeeUser, "Employee@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(employeeUser, "employer"); // or "employee" if you prefer
                }
            }

            await dbContext.SaveChangesAsync();

            // Now add domain-level seeds:

            // 1) Seed Driver
            if (!dbContext.Drivers.Any())
            {
                var user = await userManager.FindByEmailAsync("employee@example.com");
                if (user != null)
                {
                    var driver = new Driver
                    {
                        Id = Guid.NewGuid(),
                        AspNetUserId = user.Id, // link to the "employee"
                        CompanyId = defaultCompanyId,
                    };
                    dbContext.Drivers.Add(driver);
                }
            }

            // 2) Seed ContactPerson
            if (!dbContext.ContactPersons.Any())
            {
                var user = await userManager.FindByEmailAsync("customer@example.com");
                if (user != null)
                {
                    var contactPerson = new ContactPerson
                    {
                        Id = Guid.NewGuid(),
                        AspNetUserId = user.Id
                    };
                    dbContext.ContactPersons.Add(contactPerson);
                }
            }

            // 3) Seed Client
            if (!dbContext.Clients.Any())
            {
                var client = new Client
                {
                    Id = Guid.NewGuid(),
                    Name = "SampleClient",
                    Tav = "Tav Sample",
                    Address = "123 Some Street",
                    Postcode = "12345",
                    City = "SampleCity",
                    Country = "SampleCountry",
                    PhoneNumber = "555-1234",
                    Email = "client@example.com",
                    Remark = "Important client",
                    CompanyId = defaultCompanyId
                };
                dbContext.Clients.Add(client);
            }

            await dbContext.SaveChangesAsync();

            // We need references from newly seeded entities, so re-fetch them:
            var seededClient = dbContext.Clients.FirstOrDefault();
            var seededContact = dbContext.ContactPersons.FirstOrDefault();
            var seededCompany = dbContext.Companies.FirstOrDefault(c => c.Id == defaultCompanyId);

            // 4) Seed ContactPersonClientCompany
            if (!dbContext.ContactPersonClientCompanies.Any() && 
                seededContact != null && seededClient != null && seededCompany != null)
            {
                var cpc = new ContactPersonClientCompany
                {
                    Id = Guid.NewGuid(),
                    ContactPersonId = seededContact.Id,
                    CompanyId = seededCompany.Id,
                    ClientId = seededClient.Id
                };
                dbContext.ContactPersonClientCompanies.Add(cpc);
            }

            // 5) Seed Rate
            if (!dbContext.Rates.Any() && seededClient != null && seededCompany != null)
            {
                var rate = new Rate
                {
                    Id = Guid.NewGuid(),
                    Name = "SampleRate",
                    Value = 100.0m,
                    ClientId = seededClient.Id,
                    CompanyId = seededCompany.Id
                };
                dbContext.Rates.Add(rate);
            }

            // 6) Seed Surcharge
            if (!dbContext.Surcharges.Any() && seededClient != null && seededCompany != null)
            {
                var surcharge = new Surcharge
                {
                    Id = Guid.NewGuid(),
                    Value = 10.5m,
                    ClientId = seededClient.Id,
                    CompanyId = seededCompany.Id
                };
                dbContext.Surcharges.Add(surcharge);
            }

            // 7) Seed Unit
            if (!dbContext.Units.Any())
            {
                var unit = new Unit
                {
                    Id = Guid.NewGuid(),
                    Value = "Hours"
                };
                dbContext.Units.Add(unit);
            }

            await dbContext.SaveChangesAsync();

            // Re-fetch references for next seeds
            var seededUnit = dbContext.Units.FirstOrDefault();
            var seededRate = dbContext.Rates.FirstOrDefault();
            var seededSurcharge = dbContext.Surcharges.FirstOrDefault();

            // 8) Seed Car
            if (!dbContext.Cars.Any() && seededCompany != null)
            {
                var car = new Car
                {
                    Id = Guid.NewGuid(),
                    LicensePlate = "ABC-123",
                    Remark = "Test Car",
                    CompanyId = seededCompany.Id
                };
                dbContext.Cars.Add(car);
            }

            await dbContext.SaveChangesAsync();

            var seededCar = dbContext.Cars.FirstOrDefault();
            var seededDriver = dbContext.Drivers.FirstOrDefault();

            // 9) Seed CarDriver
            if (!dbContext.CarDrivers.Any() && seededCar != null && seededDriver != null)
            {
                var carDriver = new CarDriver
                {
                    Id = Guid.NewGuid(),
                    CarId = seededCar.Id,
                    DriverId = seededDriver.Id
                };
                dbContext.CarDrivers.Add(carDriver);
            }

            // 10) Seed Ride
            if (!dbContext.Rides.Any())
            {
                var ride = new Ride
                {
                    Id = Guid.NewGuid(),
                    Name = "SampleRide",
                    Remark = "First ride"
                };
                dbContext.Rides.Add(ride);
            }

            // 11) Seed Charter
            if (!dbContext.Charters.Any() && seededClient != null && seededCompany != null)
            {
                var charter = new Charter
                {
                    Id = Guid.NewGuid(),
                    Name = "TestCharter",
                    ClientId = seededClient.Id,
                    CompanyId = seededCompany.Id,
                    Remark = "Charter remark"
                };
                dbContext.Charters.Add(charter);
            }

            await dbContext.SaveChangesAsync();

            var seededRide = dbContext.Rides.FirstOrDefault();
            var seededCharter = dbContext.Charters.FirstOrDefault();

            // 12) Seed PartRide
            if (!dbContext.PartRides.Any() && seededRide != null && 
                seededCar != null && seededDriver != null &&
                seededClient != null && seededUnit != null &&
                seededRate != null && seededSurcharge != null && seededCompany != null &&
                seededCharter != null)
            {
                var partRide = new PartRide
                {
                    Id = Guid.NewGuid(),
                    RideId = seededRide.Id,
                    Date = DateTime.UtcNow.Date,
                    Start = new TimeSpan(8, 0, 0),
                    End = new TimeSpan(16, 30, 0),
                    Rest = new TimeSpan(0, 30, 0),
                    Kilometers = 150,
                    CarId = seededCar.Id,
                    DriverId = seededDriver.Id,
                    Costs = 300m,
                    Employer = "EmployerName",
                    ClientId = seededClient.Id,
                    Day = DateTime.Now.Day,
                    WeekNumber = 1,
                    Hours = 8f,
                    DecimalHours = 8.0f,
                    UnitId = seededUnit.Id,
                    RateId = seededRate.Id,
                    CostsDescription = "Test cost desc",
                    SurchargeId = seededSurcharge.Id,
                    Turnover = 500m,
                    Remark = "Part ride remark",
                    CompanyId = seededCompany.Id,
                    CharterId = seededCharter.Id
                };
                dbContext.PartRides.Add(partRide);
            }

            await dbContext.SaveChangesAsync();
        }
    }
}
