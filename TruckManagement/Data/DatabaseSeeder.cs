using TruckManagement.Data;
using TruckManagement.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            // 3) Seed roles (including "client")
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
            
            // Example #2.a: A customerAdmin user
            const string customerAdminEmail = "customerAdmin@example.com";
            var customerAdminUser = await userManager.FindByEmailAsync(customerAdminEmail);
            if (customerAdminUser == null)
            {
                customerAdminUser = new ApplicationUser
                {
                    UserName = customerAdminEmail,
                    Email = customerAdminEmail,
                    FirstName = "Frank",
                    LastName = "Customer Admin",
                };

                var result = await userManager.CreateAsync(customerAdminUser, "Customer@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(customerAdminUser, "customerAdmin");
                }
            }

            // Example #3: A driver user
            const string driverEmail = "driver@example.com";
            var driverUser = await userManager.FindByEmailAsync(driverEmail);
            if (driverUser == null)
            {
                driverUser = new ApplicationUser
                {
                    UserName = driverEmail,
                    Email = driverEmail,
                    FirstName = "Emily",
                    LastName = "Driver",
                };

                var result = await userManager.CreateAsync(driverUser, "Driver@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(driverUser, "driver");
                }
            }

            // Example #4: A client user
            const string clientEmail = "client@example.com";
            var clientUser = await userManager.FindByEmailAsync(clientEmail);
            if (clientUser == null)
            {
                clientUser = new ApplicationUser
                {
                    UserName = clientEmail,
                    Email = clientEmail,
                    FirstName = "Alice",
                    LastName = "Client",
                };

                var result = await userManager.CreateAsync(clientUser, "Client@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(clientUser, "employer");
                }
            }

            await dbContext.SaveChangesAsync();

            // 7) Seed Driver for driverUser
            if (!dbContext.Drivers.Any(d => d.AspNetUserId == driverUser.Id))
            {
                var driver = new Driver
                {
                    Id = Guid.NewGuid(),
                    AspNetUserId = driverUser.Id, // link to the "driver"
                    CompanyId = defaultCompanyId,
                };
                dbContext.Drivers.Add(driver);
            }

            // 8) Seed ContactPerson for customerUser
            if (!dbContext.ContactPersons.Any(cp => cp.AspNetUserId == customerUser.Id))
            {
                var contactPerson = new ContactPerson
                {
                    Id = Guid.NewGuid(),
                    AspNetUserId = customerUser.Id
                };
                dbContext.ContactPersons.Add(contactPerson);
            }
            
            // 8) Seed ContactPerson for customerAdminUser
            if (!dbContext.ContactPersons.Any(cp => cp.AspNetUserId == customerAdminUser.Id))
            {
                var contactPersonCustomerAdmin = new ContactPerson
                {
                    Id = Guid.NewGuid(),
                    AspNetUserId = customerAdminUser.Id
                };
                dbContext.ContactPersons.Add(contactPersonCustomerAdmin);
            }

            // 9) Seed ContactPerson for adminUser (Global Admin as ContactPerson)
            if (!dbContext.ContactPersons.Any(cp => cp.AspNetUserId == adminUser.Id))
            {
                var adminContactPerson = new ContactPerson
                {
                    Id = Guid.NewGuid(),
                    AspNetUserId = adminUser.Id
                };
                dbContext.ContactPersons.Add(adminContactPerson);
            }

            // 10) Seed ContactPerson for clientUser
            if (!dbContext.ContactPersons.Any(cp => cp.AspNetUserId == clientUser.Id))
            {
                var clientContactPerson = new ContactPerson
                {
                    Id = Guid.NewGuid(),
                    AspNetUserId = clientUser.Id
                };
                dbContext.ContactPersons.Add(clientContactPerson);
            }

            await dbContext.SaveChangesAsync();

            // 11) Seed Clients with hardcoded and memorable IDs
            var clientsToSeed = new List<Client>
            {
                new Client
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Name = "AlphaCorp",
                    Tav = "TAV-ALPHA",
                    Address = "100 Alpha Street",
                    Postcode = "10001",
                    City = "Alphaville",
                    Country = "AlphaLand",
                    PhoneNumber = "555-0001",
                    Email = "contact@alphacorp.com",
                    Remark = "Top-tier client",
                    CompanyId = defaultCompanyId
                },
                new Client
                {
                    Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    Name = "BetaIndustries",
                    Tav = "TAV-BETA",
                    Address = "200 Beta Avenue",
                    Postcode = "20002",
                    City = "Betatown",
                    Country = "BetaLand",
                    PhoneNumber = "555-0002",
                    Email = "info@betaindustries.com",
                    Remark = "Valued client",
                    CompanyId = defaultCompanyId
                },
                new Client
                {
                    Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                    Name = "GammaSolutions",
                    Tav = "TAV-GAMMA",
                    Address = "300 Gamma Road",
                    Postcode = "30003",
                    City = "Gammatown",
                    Country = "GammaLand",
                    PhoneNumber = "555-0003",
                    Email = "support@gammasolutions.com",
                    Remark = "Strategic partner",
                    CompanyId = defaultCompanyId
                }
            };

            foreach (var client in clientsToSeed)
            {
                if (!dbContext.Clients.Any(c => c.Id == client.Id))
                {
                    dbContext.Clients.Add(client);
                }
            }

            await dbContext.SaveChangesAsync();

            // 12) Associate client user with clients via ContactPersonClientCompany
            var seededClientUserContact = dbContext.ContactPersons.FirstOrDefault(cp => cp.AspNetUserId == clientUser.Id);
            if (seededClientUserContact != null)
            {
                foreach (var client in clientsToSeed)
                {
                    if (!dbContext.ContactPersonClientCompanies.Any(cpc =>
                        cpc.ContactPersonId == seededClientUserContact.Id &&
                        cpc.ClientId == client.Id))
                    {
                        var cpc = new ContactPersonClientCompany
                        {
                            Id = Guid.NewGuid(),
                            ContactPersonId = seededClientUserContact.Id,
                            CompanyId = client.CompanyId, // Assuming client is linked to a company
                            ClientId = client.Id
                        };
                        dbContext.ContactPersonClientCompanies.Add(cpc);
                    }
                }
            }

            // 13) Add admin and customer ContactPersons to companies
            // Associate adminUser with the default company
            if (adminUser != null)
            {
                var adminContact = dbContext.ContactPersons.FirstOrDefault(cp => cp.AspNetUserId == adminUser.Id);
                if (adminContact != null && !dbContext.ContactPersonClientCompanies.Any(cpc =>
                        cpc.ContactPersonId == adminContact.Id && cpc.CompanyId == defaultCompanyId))
                {
                    dbContext.ContactPersonClientCompanies.Add(new ContactPersonClientCompany
                    {
                        Id = Guid.NewGuid(),
                        ContactPersonId = adminContact.Id,
                        CompanyId = defaultCompanyId,
                        ClientId = null // Admins are only associated with companies, not clients
                    });
                }
            }
            
            if (customerUser != null)
            {
                var customerContact = dbContext.ContactPersons.FirstOrDefault(cp => cp.AspNetUserId == customerUser.Id);
                if (customerContact != null && !dbContext.ContactPersonClientCompanies.Any(cpc =>
                        cpc.ContactPersonId == customerContact.Id && cpc.CompanyId == Guid.Parse("22222222-2222-2222-2222-222222222222")))
                {
                    dbContext.ContactPersonClientCompanies.Add(new ContactPersonClientCompany
                    {
                        Id = Guid.NewGuid(),
                        ContactPersonId = customerContact.Id,
                        CompanyId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        ClientId = null // Customers are only associated with companies, not clients
                    });
                }
            }
            
            if (customerAdminUser != null)
            {
                var customerAdminContact = dbContext.ContactPersons.FirstOrDefault(cp => cp.AspNetUserId == customerAdminUser.Id);
                if (customerAdminContact != null && !dbContext.ContactPersonClientCompanies.Any(cpc =>
                        cpc.ContactPersonId == customerAdminContact.Id && cpc.CompanyId == Guid.Parse("22222222-2222-2222-2222-222222222222")))
                {
                    dbContext.ContactPersonClientCompanies.Add(new ContactPersonClientCompany
                    {
                        Id = Guid.NewGuid(),
                        ContactPersonId = customerAdminContact.Id,
                        CompanyId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        ClientId = null // Customer admins are only associated with companies
                    });
                }
            }
            
            await dbContext.SaveChangesAsync();

            // 14) Seed Rates with fixed GUIDs
            if (!dbContext.Rates.Any())
            {
                var rate1 = new Rate
                {
                    Id = Guid.NewGuid(),
                    Name = "StandardRate",
                    Value = 120.0m,
                    ClientId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    CompanyId = defaultCompanyId
                };
                var rate2 = new Rate
                {
                    Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                    Name = "PremiumRate",
                    Value = 200.0m,
                    ClientId = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    CompanyId = defaultCompanyId
                };
                dbContext.Rates.AddRange(rate1, rate2);
            }

            // 15) Seed Surcharges with fixed GUIDs
            if (!dbContext.Surcharges.Any())
            {
                var surcharge1 = new Surcharge
                {
                    Id = Guid.NewGuid(),
                    Value = 15.0m,
                    ClientId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    CompanyId = defaultCompanyId
                };
                var surcharge2 = new Surcharge
                {
                    Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                    Value = 25.0m,
                    ClientId = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    CompanyId = defaultCompanyId
                };
                dbContext.Surcharges.AddRange(surcharge1, surcharge2);
            }

            // 16) Seed Units with fixed GUIDs
            if (!dbContext.Units.Any())
            {
                var unit1 = new Unit
                {
                    Id = Guid.NewGuid(),
                    Value = "Kilometers"
                };
                var unit2 = new Unit
                {
                    Id = Guid.Parse("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"),
                    Value = "Hours"
                };
                dbContext.Units.AddRange(unit1, unit2);
            }

            await dbContext.SaveChangesAsync();

            // 17) Seed Cars with fixed GUIDs
            if (!dbContext.Cars.Any())
            {
                var car1 = new Car
                {
                    Id = Guid.NewGuid(),
                    LicensePlate = "XYZ-789",
                    Remark = "Delivery Van",
                    CompanyId = defaultCompanyId
                };
                var car2 = new Car
                {
                    Id = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB"),
                    LicensePlate = "LMN-456",
                    Remark = "Transport Truck",
                    CompanyId = defaultCompanyId
                };
                dbContext.Cars.AddRange(car1, car2);
            }

            await dbContext.SaveChangesAsync();

            var seededCar1Entity = dbContext.Cars.FirstOrDefault(c => c.LicensePlate == "XYZ-789");
            var seededCar2Entity = dbContext.Cars.FirstOrDefault(c => c.LicensePlate == "LMN-456");
            var seededDriverEntity = dbContext.Drivers.FirstOrDefault(d => d.AspNetUserId == driverUser.Id);

            // 18) Seed CarDrivers
            if (seededCar1Entity != null && seededDriverEntity != null && !dbContext.CarDrivers.Any(cd => cd.CarId == seededCar1Entity.Id && cd.DriverId == seededDriverEntity.Id))
            {
                var carDriver = new CarDriver
                {
                    Id = Guid.NewGuid(),
                    CarId = seededCar1Entity.Id,
                    DriverId = seededDriverEntity.Id
                };
                dbContext.CarDrivers.Add(carDriver);
            }

            if (seededCar2Entity != null && seededDriverEntity != null && !dbContext.CarDrivers.Any(cd => cd.CarId == seededCar2Entity.Id && cd.DriverId == seededDriverEntity.Id))
            {
                var carDriver = new CarDriver
                {
                    Id = Guid.NewGuid(),
                    CarId = seededCar2Entity.Id,
                    DriverId = seededDriverEntity.Id
                };
                dbContext.CarDrivers.Add(carDriver);
            }

            await dbContext.SaveChangesAsync();

            // 19) Seed Rides with fixed GUIDs
            if (!dbContext.Rides.Any())
            {
                var ride1 = new Ride
                {
                    Id = Guid.NewGuid(),
                    Name = "MorningDelivery",
                    Remark = "Deliver goods in the morning"
                };
                var ride2 = new Ride
                {
                    Id = Guid.Parse("CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC"),
                    Name = "EveningTransport",
                    Remark = "Transport goods in the evening"
                };
                dbContext.Rides.AddRange(ride1, ride2);
            }

            await dbContext.SaveChangesAsync();

            var seededRide1 = dbContext.Rides.FirstOrDefault(r => r.Name == "MorningDelivery");
            var seededRide2 = dbContext.Rides.FirstOrDefault(r => r.Name == "EveningTransport");

            // 20) Seed Charters with fixed GUIDs
            if (!dbContext.Charters.Any())
            {
                var charter1 = new Charter
                {
                    Id = Guid.NewGuid(),
                    Name = "CharterAlpha",
                    ClientId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    CompanyId = defaultCompanyId,
                    Remark = "Alpha charter operations"
                };
                var charter2 = new Charter
                {
                    Id = Guid.Parse("DDDDDDDD-DDDD-DDDD-DDDD-DDDDDDDDDDDD"),
                    Name = "CharterBeta",
                    ClientId = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    CompanyId = defaultCompanyId,
                    Remark = "Beta charter operations"
                };
                dbContext.Charters.AddRange(charter1, charter2);
            }

            await dbContext.SaveChangesAsync();

            var seededCharter1 = dbContext.Charters.FirstOrDefault(c => c.Name == "CharterAlpha");
            var seededCharter2 = dbContext.Charters.FirstOrDefault(c => c.Name == "CharterBeta");

            // 21) Seed PartRides
            if (!dbContext.PartRides.Any())
            {
                // PartRide 1
                if (seededRide1 != null &&
                    seededCar1Entity != null &&
                    seededDriverEntity != null &&
                    dbContext.Units.Any() &&
                    dbContext.Rates.Any() &&
                    dbContext.Surcharges.Any() &&
                    seededCharter1 != null)
                {
                    var seededUnit = dbContext.Units.FirstOrDefault(u => u.Value == "Hours");
                    var seededRate = dbContext.Rates.FirstOrDefault(r => r.Name == "StandardRate");
                    var seededSurcharge = dbContext.Surcharges.FirstOrDefault(s => s.Value == 15.0m);

                    var partRide1 = new PartRide
                    {
                        Id = Guid.NewGuid(),
                        RideId = seededRide1.Id,
                        Date = DateTime.UtcNow.Date,
                        Start = new TimeSpan(8, 0, 0),
                        End = new TimeSpan(16, 30, 0),
                        Rest = new TimeSpan(0, 30, 0),
                        Kilometers = 150,
                        CarId = seededCar1Entity.Id,
                        DriverId = seededDriverEntity.Id,
                        Costs = 300m,
                        Employer = "AlphaEmployer",
                        ClientId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                        Day = DateTime.Now.Day,
                        WeekNumber = 1,
                        Hours = 8f,
                        DecimalHours = 8.0f,
                        UnitId = seededUnit != null ? seededUnit.Id : Guid.Empty,
                        RateId = seededRate != null ? seededRate.Id : Guid.Empty,
                        CostsDescription = "Delivery costs",
                        SurchargeId = seededSurcharge != null ? seededSurcharge.Id : Guid.Empty,
                        Turnover = 500m,
                        Remark = "Morning delivery ride",
                        CompanyId = defaultCompanyId,
                        CharterId = seededCharter1.Id
                    };
                    dbContext.PartRides.Add(partRide1);
                }

                // PartRide 2
                if (seededRide2 != null &&
                    seededCar2Entity != null &&
                    seededDriverEntity != null &&
                    dbContext.Units.Any() &&
                    dbContext.Rates.Any() &&
                    dbContext.Surcharges.Any() &&
                    seededCharter2 != null)
                {
                    var seededUnit = dbContext.Units.FirstOrDefault(u => u.Value == "Hours");
                    var seededRate = dbContext.Rates.FirstOrDefault(r => r.Name == "PremiumRate");
                    var seededSurcharge = dbContext.Surcharges.FirstOrDefault(s => s.Value == 25.0m);

                    var partRide2 = new PartRide
                    {
                        Id = Guid.Parse("EEEEEEEE-EEEE-EEEE-EEEE-EEEEEEEEEEEE"),
                        RideId = seededRide2.Id,
                        Date = DateTime.UtcNow.Date,
                        Start = new TimeSpan(17, 0, 0),
                        End = new TimeSpan(23, 30, 0),
                        Rest = new TimeSpan(0, 45, 0),
                        Kilometers = 200,
                        CarId = seededCar2Entity.Id,
                        DriverId = seededDriverEntity.Id,
                        Costs = 450m,
                        Employer = "BetaEmployer",
                        ClientId = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                        Day = DateTime.Now.Day,
                        WeekNumber = 2,
                        Hours = 6.75f,
                        DecimalHours = 6.75f,
                        UnitId = seededUnit != null ? seededUnit.Id : Guid.Empty,
                        RateId = seededRate != null ? seededRate.Id : Guid.Empty,
                        CostsDescription = "Transport costs",
                        SurchargeId = seededSurcharge != null ? seededSurcharge.Id : Guid.Empty,
                        Turnover = 800m,
                        Remark = "Evening transport ride",
                        CompanyId = defaultCompanyId,
                        CharterId = seededCharter2.Id
                    };
                    dbContext.PartRides.Add(partRide2);
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }
}
