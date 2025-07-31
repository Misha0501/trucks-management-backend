using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TruckManagement.Data;
using TruckManagement.Data.Seeding;
using TruckManagement.Entities;

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
            var roles = new[]
                { "driver", "employer", "customer", "customerAdmin", "customerAccountant", "globalAdmin" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new ApplicationRole { Name = role });
                }
            }

            // 4) Seed the default company if it doesn't exist
            var defaultCompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            if (!dbContext.Companies.IgnoreQueryFilters().Any(c => c.Id == defaultCompanyId))
            {
                dbContext.Companies.Add(new Company
                {
                    Id = defaultCompanyId,
                    Name = "DefaultCompany",
                    IsApproved = true
                });
                await dbContext.SaveChangesAsync();
            }

            // 5) Seed additional companies
            var companiesToSeed = new List<Company>
            {
                new Company
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "SecondCompany",
                    IsApproved = true
                },
                new Company
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Name = "ThirdCompany",
                    IsApproved = true
                },
                new Company
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Name = "FourthCompany",
                    IsApproved = true
                },
                new Company
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Name = "5 Global Express",
                    IsApproved = true
                }
            };

            foreach (var comp in companiesToSeed)
            {
                if (!dbContext.Companies.IgnoreQueryFilters().Any(c => c.Id == comp.Id))
                {
                    dbContext.Companies.Add(comp);
                }
            }

            await dbContext.SaveChangesAsync();

            // 6) Seed sample users
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Example #1: A globalAdmin user
            const string adminEmail = "admin@admin.com";
            var adminUser = await userManager.Users
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(u => u.NormalizedEmail == adminEmail.ToUpper());

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "User",
                    IsApproved = true
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
            var customerUser = await userManager.Users
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(u => u.NormalizedEmail == customerEmail.ToUpper());

            if (customerUser == null)
            {
                customerUser = new ApplicationUser
                {
                    UserName = customerEmail,
                    Email = customerEmail,
                    FirstName = "John",
                    LastName = "Customer",
                    IsApproved = true
                };

                var result = await userManager.CreateAsync(customerUser, "Customer@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(customerUser, "customer");
                }
            }

            // Example #2.a: A customerAdmin user
            const string customerAdminEmail = "customerAdmin@example.com";
            var customerAdminUser = await userManager.Users
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(u => u.NormalizedEmail == customerAdminEmail.ToUpper());

            if (customerAdminUser == null)
            {
                customerAdminUser = new ApplicationUser
                {
                    UserName = customerAdminEmail,
                    Email = customerAdminEmail,
                    FirstName = "Frank",
                    LastName = "Customer Admin",
                    IsApproved = true,
                };

                var result = await userManager.CreateAsync(customerAdminUser, "Customer@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(customerAdminUser, "customerAdmin");
                }
            }

            // Example #3: A driver user
            const string driverEmail = "driver@example.com";
            var driverUser = await userManager.Users
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(u => u.NormalizedEmail == driverEmail.ToUpper());

            if (driverUser == null)
            {
                driverUser = new ApplicationUser
                {
                    UserName = driverEmail,
                    Email = driverEmail,
                    FirstName = "Emily",
                    LastName = "Driver",
                    IsApproved = true,
                };

                var result = await userManager.CreateAsync(driverUser, "Driver@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(driverUser, "driver");
                }
            }

            // Example #4: A client user
            const string clientEmail = "client@example.com";
            var clientUser = await userManager.Users
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(u => u.NormalizedEmail == clientEmail.ToUpper());

            if (clientUser == null)
            {
                clientUser = new ApplicationUser
                {
                    UserName = clientEmail,
                    Email = clientEmail,
                    FirstName = "Alice",
                    LastName = "Client",
                    IsApproved = true
                };

                var result = await userManager.CreateAsync(clientUser, "Client@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(clientUser, "employer");
                }
            }

            await dbContext.SaveChangesAsync();

            // 7) Seed Driver for driverUser
            if (!dbContext.Drivers.IgnoreQueryFilters().Any(d => d.AspNetUserId == driverUser.Id))
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
            if (!dbContext.ContactPersons.IgnoreQueryFilters().Any(cp => cp.AspNetUserId == customerUser.Id))
            {
                var contactPerson = new ContactPerson
                {
                    Id = Guid.NewGuid(),
                    AspNetUserId = customerUser.Id
                };
                dbContext.ContactPersons.Add(contactPerson);
            }

            // 8) Seed ContactPerson for customerAdminUser
            if (!dbContext.ContactPersons.IgnoreQueryFilters().Any(cp => cp.AspNetUserId == customerAdminUser.Id))
            {
                var contactPersonCustomerAdmin = new ContactPerson
                {
                    Id = Guid.NewGuid(),
                    AspNetUserId = customerAdminUser.Id
                };
                dbContext.ContactPersons.Add(contactPersonCustomerAdmin);
            }

            // 9) Seed ContactPerson for adminUser (Global Admin as ContactPerson)
            if (!dbContext.ContactPersons.IgnoreQueryFilters().Any(cp => cp.AspNetUserId == adminUser.Id))
            {
                var adminContactPerson = new ContactPerson
                {
                    Id = Guid.NewGuid(),
                    AspNetUserId = adminUser.Id
                };
                dbContext.ContactPersons.Add(adminContactPerson);
            }

            // 10) Seed ContactPerson for clientUser
            if (!dbContext.ContactPersons.IgnoreQueryFilters().Any(cp => cp.AspNetUserId == clientUser.Id))
            {
                var clientContactPerson = new ContactPerson
                {
                    Id = Guid.NewGuid(),
                    AspNetUserId = clientUser.Id
                };
                dbContext.ContactPersons.Add(clientContactPerson);
            }

            await dbContext.SaveChangesAsync();

            // 2) Seed HoursOptions
            var hoursOptionsToSeed = new List<HoursOption>
            {
                new HoursOption
                    { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "StandOver", IsActive = true },
                new HoursOption
                    { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Holiday", IsActive = true },
                new HoursOption
                    { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "NoHoliday", IsActive = true },
                new HoursOption
                    { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "NoAllowance", IsActive = true },
                new HoursOption
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "NoCommutingAllowance",
                    IsActive = true
                },
                new HoursOption
                {
                    Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), Name = "NoNightAllowance", IsActive = true
                }
                // add more if needed
            };

            foreach (var ho in hoursOptionsToSeed)
            {
                if (!dbContext.HoursOptions.Any(x => x.Id == ho.Id))
                {
                    dbContext.HoursOptions.Add(ho);
                }
            }

            // 3) Seed HoursCodes
            var hoursCodesToSeed = new List<HoursCode>
            {
                new HoursCode
                    { Id = Guid.Parse("AAAA1111-1111-1111-1111-111111111111"), Name = "One day ride", IsActive = true },
                new HoursCode
                {
                    Id = Guid.Parse("AAAA2222-2222-2222-2222-222222222222"), Name = "Multi-day trip departure",
                    IsActive = true
                },
                new HoursCode
                {
                    Id = Guid.Parse("AAAA3333-3333-3333-3333-333333333333"), Name = "Multi-day trip intermediate day",
                    IsActive = true
                },
                new HoursCode
                {
                    Id = Guid.Parse("AAAA4444-4444-4444-4444-444444444444"), Name = "Multi-day trip arrival",
                    IsActive = true
                },
                new HoursCode
                    { Id = Guid.Parse("AAAA5555-5555-5555-5555-555555555555"), Name = "Holiday", IsActive = true },
                new HoursCode
                    { Id = Guid.Parse("AAAA6666-6666-6666-6666-666666666666"), Name = "Sick", IsActive = true },
                new HoursCode
                {
                    Id = Guid.Parse("AAAA7777-7777-7777-7777-777777777777"), Name = "Time for time", IsActive = true
                },
                new HoursCode
                    { Id = Guid.Parse("AAAA8888-8888-8888-8888-888888888888"), Name = "Other work", IsActive = true },
                new HoursCode
                    { Id = Guid.Parse("AAAA9999-9999-9999-9999-999999999999"), Name = "Course day", IsActive = true },
                new HoursCode
                    { Id = Guid.Parse("AAAABBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB"), Name = "Consignment", IsActive = true },
                new HoursCode
                    { Id = Guid.Parse("AAAACCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC"), Name = "Unpaid", IsActive = true },
            };

            foreach (var hc in hoursCodesToSeed)
            {
                if (!dbContext.HoursCodes.Any(x => x.Id == hc.Id))
                {
                    dbContext.HoursCodes.Add(hc);
                }
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
                    CompanyId = defaultCompanyId,
                    IsApproved = true,
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
                    CompanyId = defaultCompanyId,
                    IsApproved = true,
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
                    CompanyId = defaultCompanyId,
                    IsApproved = true,
                },
                new Client
                {
                    Id = Guid.Parse("78777777-7777-7777-7777-777777777777"),
                    Name = "78 Express Solutions",
                    Tav = "TAV-Express",
                    Address = "300 Express Road",
                    Postcode = "30003",
                    City = "Den Haag",
                    Country = "Netherlands",
                    PhoneNumber = "555-0003",
                    Email = "support@express.com",
                    Remark = "Strategic partner",
                    CompanyId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    IsApproved = true,
                }
            };

            foreach (var client in clientsToSeed)
            {
                if (!dbContext.Clients.IgnoreQueryFilters().Any(c => c.Id == client.Id))
                {
                    dbContext.Clients.Add(client);
                }
            }

            await dbContext.SaveChangesAsync();

            // 12) Associate client user with clients via ContactPersonClientCompany
            var seededClientUserContact =
                dbContext.ContactPersons.IgnoreQueryFilters().FirstOrDefault(cp => cp.AspNetUserId == clientUser.Id);
            if (seededClientUserContact != null)
            {
                foreach (var client in clientsToSeed)
                {
                    if (!dbContext.ContactPersonClientCompanies.IgnoreQueryFilters().Any(cpc =>
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
                var adminContact = dbContext.ContactPersons.IgnoreQueryFilters()
                    .FirstOrDefault(cp => cp.AspNetUserId == adminUser.Id);
                if (adminContact != null && !dbContext.ContactPersonClientCompanies.IgnoreQueryFilters().Any(cpc =>
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
                var customerContact = dbContext.ContactPersons.IgnoreQueryFilters()
                    .FirstOrDefault(cp => cp.AspNetUserId == customerUser.Id);
                if (customerContact != null && !dbContext.ContactPersonClientCompanies.IgnoreQueryFilters().Any(cpc =>
                        cpc.ContactPersonId == customerContact.Id &&
                        cpc.CompanyId == Guid.Parse("22222222-2222-2222-2222-222222222222")))
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
                var customerAdminContact =
                    dbContext.ContactPersons.IgnoreQueryFilters()
                        .FirstOrDefault(cp => cp.AspNetUserId == customerAdminUser.Id);
                if (customerAdminContact != null && !dbContext.ContactPersonClientCompanies.IgnoreQueryFilters().Any(
                        cpc =>
                            cpc.ContactPersonId == customerAdminContact.Id &&
                            cpc.CompanyId == Guid.Parse("22222222-2222-2222-2222-222222222222")))
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
            if (!dbContext.Rates.IgnoreQueryFilters().Any())
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
            if (!dbContext.Surcharges.IgnoreQueryFilters().Any())
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
            if (!dbContext.Units.IgnoreQueryFilters().Any())
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
            if (!dbContext.Cars.IgnoreQueryFilters().Any())
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

            var seededCar1Entity = dbContext.Cars.IgnoreQueryFilters().FirstOrDefault(c => c.LicensePlate == "XYZ-789");
            var seededCar2Entity = dbContext.Cars.IgnoreQueryFilters().FirstOrDefault(c => c.LicensePlate == "LMN-456");
            var seededDriverEntity = dbContext.Drivers.IgnoreQueryFilters()
                .FirstOrDefault(d => d.AspNetUserId == driverUser.Id);

            // Seed DriverCompensationSettings for the seeded driver
            if (seededDriverEntity != null &&
                !dbContext.DriverCompensationSettings.Any(dcs => dcs.DriverId == seededDriverEntity.Id))
            {
                var settings = new DriverCompensationSettings
                {
                    Id = Guid.NewGuid(),
                    DriverId = seededDriverEntity.Id,
                    PercentageOfWork = 100,
                    NightHoursAllowed = true,
                    NightHours19Percent = true,
                    DriverRatePerHour = 18.71m,
                    NightAllowanceRate = 0.19m,
                    KilometerAllowanceEnabled = true,
                    KilometersOneWayValue = 25,
                    KilometersMin = 10,
                    KilometersMax = 35,
                    KilometerAllowance = 0.23m,
                    Salary4Weeks = 2000m,
                    WeeklySalary = 500m,
                    DateOfEmployment = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                };

                dbContext.DriverCompensationSettings.Add(settings);
                await dbContext.SaveChangesAsync();
            }

            // 18) Assign Car to Driver (1-1 relationship)
            // Assign the first car to the seeded driver
            if (seededCar1Entity != null && seededDriverEntity != null && seededDriverEntity.CarId == null)
            {
                seededDriverEntity.CarId = seededCar1Entity.Id;
                await dbContext.SaveChangesAsync();
            }

            // 19) Seed Rides with fixed GUIDs
            if (!dbContext.Rides.IgnoreQueryFilters().Any())
            {
                var ride1 = new Ride
                {
                    Id = Guid.NewGuid(),
                    Name = "MorningDelivery",
                    Remark = "Deliver goods in the morning",
                    CompanyId = defaultCompanyId
                };
                var ride2 = new Ride
                {
                    Id = Guid.Parse("CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC"),
                    Name = "EveningTransport",
                    Remark = "Transport goods in the evening",
                    CompanyId = defaultCompanyId
                };
                dbContext.Rides.AddRange(ride1, ride2);
            }

            await dbContext.SaveChangesAsync();

            var seededRide1 = dbContext.Rides.IgnoreQueryFilters().FirstOrDefault(r => r.Name == "MorningDelivery");
            var seededRide2 = dbContext.Rides.IgnoreQueryFilters().FirstOrDefault(r => r.Name == "EveningTransport");

            // 20) Seed Charters with fixed GUIDs
            if (!dbContext.Charters.IgnoreQueryFilters().Any())
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

            var seededCharter1 = dbContext.Charters.IgnoreQueryFilters().FirstOrDefault(c => c.Name == "CharterAlpha");
            var seededCharter2 = dbContext.Charters.IgnoreQueryFilters().FirstOrDefault(c => c.Name == "CharterBeta");

            // 21) Seed PartRides
            if (!dbContext.PartRides.IgnoreQueryFilters().Any())
            {
                // PartRide 1
                if (seededRide1 != null &&
                    seededCar1Entity != null &&
                    seededDriverEntity != null &&
                    dbContext.Units.IgnoreQueryFilters().Any() &&
                    dbContext.Rates.IgnoreQueryFilters().Any() &&
                    dbContext.Surcharges.IgnoreQueryFilters().Any() &&
                    seededCharter1 != null)
                {
                    var seededUnit = dbContext.Units.IgnoreQueryFilters().FirstOrDefault(u => u.Value == "Hours");
                    var seededRate = dbContext.Rates.IgnoreQueryFilters().FirstOrDefault(r => r.Name == "StandardRate");
                    var seededSurcharge =
                        dbContext.Surcharges.IgnoreQueryFilters().FirstOrDefault(s => s.Value == 15.0m);

                    var partRide1 = new PartRide
                    {
                        Id = Guid.NewGuid(),
                        RideId = seededRide1.Id,
                        Date = DateTime.UtcNow.Date,
                        Start = new TimeSpan(8, 0, 0),
                        End = new TimeSpan(16, 30, 0),
                        Rest = new TimeSpan(0, 30, 0),
                        TotalKilometers = 150,
                        CarId = seededCar1Entity.Id,
                        DriverId = seededDriverEntity.Id,
                        Costs = 300m,
                        ClientId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                        WeekNumber = 1,
                        DecimalHours = 8.0f,
                        CostsDescription = "Delivery costs",
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
                    dbContext.Units.IgnoreQueryFilters().Any() &&
                    dbContext.Rates.IgnoreQueryFilters().Any() &&
                    dbContext.Surcharges.IgnoreQueryFilters().Any() &&
                    seededCharter2 != null)
                {
                    var seededUnit = dbContext.Units.IgnoreQueryFilters().FirstOrDefault(u => u.Value == "Hours");
                    var seededRate = dbContext.Rates.IgnoreQueryFilters().FirstOrDefault(r => r.Name == "PremiumRate");
                    var seededSurcharge =
                        dbContext.Surcharges.IgnoreQueryFilters().FirstOrDefault(s => s.Value == 25.0m);

                    var partRide2 = new PartRide
                    {
                        Id = Guid.Parse("EEEEEEEE-EEEE-EEEE-EEEE-EEEEEEEEEEEE"),
                        RideId = seededRide2.Id,
                        Date = DateTime.UtcNow.Date,
                        Start = new TimeSpan(17, 0, 0),
                        End = new TimeSpan(23, 30, 0),
                        Rest = new TimeSpan(0, 45, 0),
                        TotalKilometers = 200,
                        CarId = seededCar2Entity.Id,
                        DriverId = seededDriverEntity.Id,
                        Costs = 450m,
                        ClientId = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                        WeekNumber = 2,
                        DecimalHours = 6.75f,
                        CostsDescription = "Transport costs",
                        Turnover = 800m,
                        Remark = "Evening transport ride",
                        CompanyId = defaultCompanyId,
                        CharterId = seededCharter2.Id
                    };
                    dbContext.PartRides.Add(partRide2);
                }
            }

            await dbContext.SaveChangesAsync();
            // updated all drivers with DriverCompensationSettings
            var existingDrivers = dbContext.Drivers.IgnoreQueryFilters().ToList();

            foreach (var driver in existingDrivers)
            {
                var hasSettings = dbContext.DriverCompensationSettings.Any(s => s.DriverId == driver.Id);
                if (!hasSettings)
                {
                    dbContext.DriverCompensationSettings.Add(new DriverCompensationSettings
                    {
                        DriverId = driver.Id,
                        PercentageOfWork = 100,
                        NightHoursAllowed = true,
                        NightHours19Percent = false,
                        DriverRatePerHour = 18.71m,
                        NightAllowanceRate = 0.19m,
                        KilometerAllowanceEnabled = true,
                        KilometersOneWayValue = 25,
                        KilometersMin = 10,
                        KilometersMax = 35,
                        KilometerAllowance = 0.23m,
                        Salary4Weeks = 2400.00m,
                        WeeklySalary = 600.00m,
                        DateOfEmployment = DateTime.UtcNow
                    });
                }
            }
            await dbContext.SaveChangesAsync();
            
            // 22) Seed PartRides with vacation hours for testing (2025 data)
            await SeedVacationTestData(dbContext, seededDriverEntity);
            
            // 23) Seed signed weeks and periods for report testing
            await SeedSignedWeeksTestData(dbContext, seededDriverEntity);
            
            await CaoSeeder.SeedAsync(dbContext);
            
            await VacationRightSeeder.SeedAsync(dbContext);
        }

        private static async Task SeedVacationTestData(ApplicationDbContext dbContext, Driver? seededDriver)
        {
            if (seededDriver == null) return;

            var existingVacationTestData = dbContext.PartRides
                .IgnoreQueryFilters()
                .Any(pr => pr.Remark != null && pr.Remark.Contains("VACATION_TEST_DATA"));
            
            if (existingVacationTestData) return; // Already seeded

            var currentYear = DateTime.UtcNow.Year;
            var testDataRides = new List<PartRide>();

            // Get an existing car and client for the test data
            var seededCar = dbContext.Cars.IgnoreQueryFilters().FirstOrDefault();
            var seededClient = dbContext.Clients.IgnoreQueryFilters().FirstOrDefault();
            var defaultCompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111");

            if (seededCar == null || seededClient == null) return;

            // 1. Work days with positive vacation hours (earned)
            for (int i = 1; i <= 20; i++)
            {
                testDataRides.Add(new PartRide
                {
                    Id = Guid.NewGuid(),
                    Date = DateTime.SpecifyKind(new DateTime(currentYear, 1, i), DateTimeKind.Utc), // January 2025
                    Start = new TimeSpan(8, 0, 0),
                    End = new TimeSpan(16, 0, 0),
                    Rest = new TimeSpan(0, 30, 0),
                    TotalKilometers = 100,
                    CarId = seededCar.Id,
                    DriverId = seededDriver.Id,
                    ClientId = seededClient.Id,
                    CompanyId = defaultCompanyId,
                    DecimalHours = 8.0,
                    VacationHours = 0.32, // Earned vacation hours per work day (~8 hours of 25 days = 200 hours per year / 260 work days ≈ 0.77 per day)
                    Remark = "VACATION_TEST_DATA - Work day with earned vacation",
                    Status = PartRideStatus.Accepted
                });
            }

            // 2. More work days to accumulate vacation hours
            for (int i = 1; i <= 15; i++)
            {
                testDataRides.Add(new PartRide
                {
                    Id = Guid.NewGuid(),
                    Date = DateTime.SpecifyKind(new DateTime(currentYear, 2, i), DateTimeKind.Utc), // February 2025
                    Start = new TimeSpan(9, 0, 0),
                    End = new TimeSpan(17, 0, 0),
                    Rest = new TimeSpan(0, 45, 0),
                    TotalKilometers = 120,
                    CarId = seededCar.Id,
                    DriverId = seededDriver.Id,
                    ClientId = seededClient.Id,
                    CompanyId = defaultCompanyId,
                    DecimalHours = 7.25,
                    VacationHours = 0.29, // Different amount for variety
                    Remark = "VACATION_TEST_DATA - Work day with earned vacation",
                    Status = PartRideStatus.Accepted
                });
            }

            // 3. Holiday/Vacation days with negative vacation hours (used)
            testDataRides.Add(new PartRide
            {
                Id = Guid.NewGuid(),
                Date = DateTime.SpecifyKind(new DateTime(currentYear, 2, 20), DateTimeKind.Utc), // February 20, 2025
                Start = new TimeSpan(0, 0, 0),
                End = new TimeSpan(0, 0, 0),
                Rest = new TimeSpan(0, 0, 0),
                TotalKilometers = 0,
                CarId = seededCar.Id,
                DriverId = seededDriver.Id,
                ClientId = seededClient.Id,
                CompanyId = defaultCompanyId,
                DecimalHours = 8.0,
                VacationHours = -8.0, // Used 8 hours of vacation
                Remark = "VACATION_TEST_DATA - Vacation day (used vacation hours)",
                Status = PartRideStatus.Accepted
            });

            // 4. Another vacation day
            testDataRides.Add(new PartRide
            {
                Id = Guid.NewGuid(),
                Date = DateTime.SpecifyKind(new DateTime(currentYear, 2, 21), DateTimeKind.Utc), // February 21, 2025
                Start = new TimeSpan(0, 0, 0),
                End = new TimeSpan(0, 0, 0),
                Rest = new TimeSpan(0, 0, 0),
                TotalKilometers = 0,
                CarId = seededCar.Id,
                DriverId = seededDriver.Id,
                ClientId = seededClient.Id,
                CompanyId = defaultCompanyId,
                DecimalHours = 8.0,
                VacationHours = -8.0, // Used another 8 hours of vacation
                Remark = "VACATION_TEST_DATA - Vacation day (used vacation hours)",
                Status = PartRideStatus.Accepted
            });

            // Add all test data
            dbContext.PartRides.AddRange(testDataRides);
            await dbContext.SaveChangesAsync();

            Console.WriteLine($"✅ Added {testDataRides.Count} PartRides with vacation test data for driver {seededDriver.Id}");
            Console.WriteLine($"📊 Expected vacation balance: {testDataRides.Sum(pr => pr.VacationHours):F2} hours");
        }

        private static async Task SeedSignedWeeksTestData(ApplicationDbContext dbContext, Driver? seededDriver)
        {
            if (seededDriver == null) return;

            var existingSignedWeekData = dbContext.WeekApprovals
                .IgnoreQueryFilters()
                .Any(wa => wa.DriverId == seededDriver.Id && wa.Status == WeekApprovalStatus.Signed);
            
            if (existingSignedWeekData) return; // Already seeded

            var currentYear = DateTime.UtcNow.Year;
            var defaultCompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            
            // Get existing resources
            var seededCar = dbContext.Cars.IgnoreQueryFilters().FirstOrDefault();
            var seededClient = dbContext.Clients.IgnoreQueryFilters().FirstOrDefault();
            var regularHoursCode = dbContext.HoursCodes.IgnoreQueryFilters()
                .FirstOrDefault(hc => hc.Name == "One day ride");
            var timeForTimeCode = dbContext.HoursCodes.IgnoreQueryFilters()
                .FirstOrDefault(hc => hc.Name == "Time for time");
            var holidayCode = dbContext.HoursCodes.IgnoreQueryFilters()
                .FirstOrDefault(hc => hc.Name == "Holiday");

            if (seededCar == null || seededClient == null || regularHoursCode == null) return;

            // Create signed weeks for period 1 (weeks 1-4) of 2025
            var weekApprovals = new List<WeekApproval>();
            var partRides = new List<PartRide>();

            for (int weekNr = 1; weekNr <= 4; weekNr++)
            {
                // Create WeekApproval with Signed status
                var weekApproval = new WeekApproval
                {
                    Id = Guid.NewGuid(),
                    WeekNr = weekNr,
                    Year = currentYear,
                    PeriodNr = 1,
                    DriverId = seededDriver.Id,
                    Status = WeekApprovalStatus.Signed,
                    DriverSignedAt = DateTime.UtcNow.AddDays(-30 + weekNr),
                    AdminAllowedAt = DateTime.UtcNow.AddDays(-25 + weekNr)
                };
                weekApprovals.Add(weekApproval);

                // Create PartRides for this week with various scenarios
                var weekStartDate = GetWeekStartDate(currentYear, weekNr);
                
                for (int dayOfWeek = 0; dayOfWeek < 7; dayOfWeek++)
                {
                    var currentDate = weekStartDate.AddDays(dayOfWeek);
                    var dayName = currentDate.DayOfWeek;
                    
                    // Skip some days to create realistic patterns
                    if (dayName == DayOfWeek.Saturday && weekNr % 2 == 0) continue;
                    if (dayName == DayOfWeek.Sunday && weekNr != 2) continue;

                    var partRide = CreateTestPartRide(
                        currentDate, 
                        weekNr, 
                        dayOfWeek + 1, 
                        seededDriver.Id, 
                        seededCar.Id, 
                        seededClient.Id, 
                        defaultCompanyId,
                        regularHoursCode.Id,
                        timeForTimeCode?.Id,
                        holidayCode?.Id,
                        weekApproval.Id);
                    
                    if (partRide != null)
                    {
                        partRides.Add(partRide);
                    }
                }
            }

            // Add additional single signed week (week 10) for single week report testing
            var singleWeekApproval = new WeekApproval
            {
                Id = Guid.NewGuid(),
                WeekNr = 10,
                Year = currentYear,
                PeriodNr = 3, // Period 3, but other weeks in period are not signed
                DriverId = seededDriver.Id,
                Status = WeekApprovalStatus.Signed,
                DriverSignedAt = DateTime.UtcNow.AddDays(-10),
                AdminAllowedAt = DateTime.UtcNow.AddDays(-8)
            };
            weekApprovals.Add(singleWeekApproval);

            // Create test data for week 10 with extreme overtime scenarios
            var week10StartDate = GetWeekStartDate(currentYear, 10);
            for (int dayOfWeek = 0; dayOfWeek < 5; dayOfWeek++) // Monday to Friday
            {
                var currentDate = week10StartDate.AddDays(dayOfWeek);
                var extremePartRide = CreateExtremeOvertimePartRide(
                    currentDate, 
                    10, 
                    dayOfWeek + 1, 
                    seededDriver.Id, 
                    seededCar.Id, 
                    seededClient.Id, 
                    defaultCompanyId,
                    regularHoursCode.Id,
                    singleWeekApproval.Id);
                
                if (extremePartRide != null)
                {
                    partRides.Add(extremePartRide);
                }
            }

            // Save all data
            dbContext.WeekApprovals.AddRange(weekApprovals);
            dbContext.PartRides.AddRange(partRides);
            await dbContext.SaveChangesAsync();

            Console.WriteLine($"✅ Added {weekApprovals.Count} signed WeekApprovals for driver {seededDriver.Id}");
            Console.WriteLine($"✅ Added {partRides.Count} PartRides for signed weeks");
            Console.WriteLine($"📊 Period 1 (weeks 1-4): Complete period available for testing");
            Console.WriteLine($"📊 Week 10: Single week with extreme overtime scenarios");
        }

        private static PartRide? CreateTestPartRide(
            DateTime date,
            int weekNr,
            int dayInWeek,
            Guid driverId,
            Guid carId,
            Guid clientId,
            Guid companyId,
            Guid regularHoursCodeId,
            Guid? timeForTimeCodeId,
            Guid? holidayCodeId,
            Guid weekApprovalId)
        {
            var dayOfWeek = date.DayOfWeek;
            
            // Create different scenarios based on day and week
            return dayOfWeek switch
            {
                DayOfWeek.Monday => CreateRegularWorkDay(date, weekNr, driverId, carId, clientId, companyId, regularHoursCodeId, weekApprovalId, 8.0),
                DayOfWeek.Tuesday => CreateOvertimeDay(date, weekNr, driverId, carId, clientId, companyId, regularHoursCodeId, weekApprovalId, 10.5), // 130% and 150%
                DayOfWeek.Wednesday => CreateRegularWorkDay(date, weekNr, driverId, carId, clientId, companyId, regularHoursCodeId, weekApprovalId, 7.5),
                DayOfWeek.Thursday => CreateNightShiftDay(date, weekNr, driverId, carId, clientId, companyId, regularHoursCodeId, weekApprovalId),
                DayOfWeek.Friday => CreateRegularWorkDay(date, weekNr, driverId, carId, clientId, companyId, regularHoursCodeId, weekApprovalId, 8.5),
                DayOfWeek.Saturday => CreateWeekendDay(date, weekNr, driverId, carId, clientId, companyId, regularHoursCodeId, weekApprovalId),
                DayOfWeek.Sunday => CreateHolidayDay(date, weekNr, driverId, carId, clientId, companyId, holidayCodeId ?? regularHoursCodeId, weekApprovalId),
                _ => null
            };
        }

        private static PartRide CreateRegularWorkDay(DateTime date, int weekNr, Guid driverId, Guid carId, Guid clientId, Guid companyId, Guid hoursCodeId, Guid weekApprovalId, double hours)
        {
            return new PartRide
            {
                Id = Guid.NewGuid(),
                Date = DateTime.SpecifyKind(date, DateTimeKind.Utc),
                Start = new TimeSpan(8, 0, 0),
                End = new TimeSpan(16, (int)((hours - 8) * 60), 0),
                Rest = new TimeSpan(0, 30, 0),
                TotalKilometers = 150,
                ExtraKilometers = 25,
                CarId = carId,
                DriverId = driverId,
                ClientId = clientId,
                CompanyId = companyId,
                WeekNumber = weekNr,
                DecimalHours = hours,
                HoursCodeId = hoursCodeId,
                VacationHours = 0.32, // Earned vacation
                NightAllowance = 0,
                KilometerReimbursement = 25 * 0.23, // 25 km * €0.23
                ConsignmentFee = 0,
                TaxFreeCompensation = 5.75,
                VariousCompensation = 0,
                Remark = $"SIGNED_WEEK_TEST_DATA - Regular work day, Week {weekNr}",
                Status = PartRideStatus.Accepted,
                WeekApprovalId = weekApprovalId
            };
        }

        private static PartRide CreateOvertimeDay(DateTime date, int weekNr, Guid driverId, Guid carId, Guid clientId, Guid companyId, Guid hoursCodeId, Guid weekApprovalId, double hours)
        {
            return new PartRide
            {
                Id = Guid.NewGuid(),
                Date = DateTime.SpecifyKind(date, DateTimeKind.Utc),
                Start = new TimeSpan(7, 0, 0),
                End = new TimeSpan(18, 30, 0),
                Rest = new TimeSpan(1, 0, 0),
                TotalKilometers = 280,
                ExtraKilometers = 50,
                CarId = carId,
                DriverId = driverId,
                ClientId = clientId,
                CompanyId = companyId,
                WeekNumber = weekNr,
                DecimalHours = hours,
                HoursCodeId = hoursCodeId,
                VacationHours = 0.42, // More vacation earned for longer day
                NightAllowance = 0,
                KilometerReimbursement = 50 * 0.23,
                ConsignmentFee = 0,
                TaxFreeCompensation = 8.50,
                VariousCompensation = 12.75,
                Remark = $"SIGNED_WEEK_TEST_DATA - Overtime day ({hours}h), Week {weekNr}",
                Status = PartRideStatus.Accepted,
                WeekApprovalId = weekApprovalId
            };
        }

        private static PartRide CreateNightShiftDay(DateTime date, int weekNr, Guid driverId, Guid carId, Guid clientId, Guid companyId, Guid hoursCodeId, Guid weekApprovalId)
        {
            return new PartRide
            {
                Id = Guid.NewGuid(),
                Date = DateTime.SpecifyKind(date, DateTimeKind.Utc),
                Start = new TimeSpan(22, 0, 0), // Night shift
                End = new TimeSpan(6, 0, 0),    // Next morning
                Rest = new TimeSpan(0, 30, 0),
                TotalKilometers = 200,
                ExtraKilometers = 35,
                CarId = carId,
                DriverId = driverId,
                ClientId = clientId,
                CompanyId = companyId,
                WeekNumber = weekNr,
                DecimalHours = 7.5,
                HoursCodeId = hoursCodeId,
                VacationHours = 0.30,
                NightAllowance = 7.5 * 0.19, // Night allowance
                KilometerReimbursement = 35 * 0.23,
                ConsignmentFee = 0,
                TaxFreeCompensation = 15.25,
                VariousCompensation = 0,
                Remark = $"SIGNED_WEEK_TEST_DATA - Night shift (200%), Week {weekNr}",
                Status = PartRideStatus.Accepted,
                WeekApprovalId = weekApprovalId
            };
        }

        private static PartRide CreateWeekendDay(DateTime date, int weekNr, Guid driverId, Guid carId, Guid clientId, Guid companyId, Guid hoursCodeId, Guid weekApprovalId)
        {
            return new PartRide
            {
                Id = Guid.NewGuid(),
                Date = DateTime.SpecifyKind(date, DateTimeKind.Utc),
                Start = new TimeSpan(9, 0, 0),
                End = new TimeSpan(15, 0, 0),
                Rest = new TimeSpan(0, 30, 0),
                TotalKilometers = 120,
                ExtraKilometers = 20,
                CarId = carId,
                DriverId = driverId,
                ClientId = clientId,
                CompanyId = companyId,
                WeekNumber = weekNr,
                DecimalHours = 5.5,
                HoursCodeId = hoursCodeId,
                VacationHours = 0.22,
                NightAllowance = 0,
                KilometerReimbursement = 20 * 0.23,
                ConsignmentFee = 25.0,
                TaxFreeCompensation = 7.50,
                VariousCompensation = 15.00,
                Remark = $"SIGNED_WEEK_TEST_DATA - Weekend work (200%), Week {weekNr}",
                Status = PartRideStatus.Accepted,
                WeekApprovalId = weekApprovalId
            };
        }

        private static PartRide CreateHolidayDay(DateTime date, int weekNr, Guid driverId, Guid carId, Guid clientId, Guid companyId, Guid hoursCodeId, Guid weekApprovalId)
        {
            return new PartRide
            {
                Id = Guid.NewGuid(),
                Date = DateTime.SpecifyKind(date, DateTimeKind.Utc),
                Start = new TimeSpan(0, 0, 0),
                End = new TimeSpan(0, 0, 0),
                Rest = new TimeSpan(0, 0, 0),
                TotalKilometers = 0,
                ExtraKilometers = 0,
                CarId = carId,
                DriverId = driverId,
                ClientId = clientId,
                CompanyId = companyId,
                WeekNumber = weekNr,
                DecimalHours = 8.0,
                HoursCodeId = hoursCodeId,
                VacationHours = -8.0, // Used vacation day
                NightAllowance = 0,
                KilometerReimbursement = 0,
                ConsignmentFee = 0,
                TaxFreeCompensation = 0,
                VariousCompensation = 0,
                Remark = $"SIGNED_WEEK_TEST_DATA - Holiday (vacation used), Week {weekNr}",
                Status = PartRideStatus.Accepted,
                WeekApprovalId = weekApprovalId
            };
        }

        private static PartRide CreateExtremeOvertimePartRide(DateTime date, int weekNr, int dayInWeek, Guid driverId, Guid carId, Guid clientId, Guid companyId, Guid hoursCodeId, Guid weekApprovalId)
        {
            // Create extreme scenarios for testing
            var hours = dayInWeek switch
            {
                1 => 12.0, // Monday: 12 hours (150%)
                2 => 14.0, // Tuesday: 14 hours (150%)
                3 => 9.5,  // Wednesday: 9.5 hours (130%)
                4 => 11.0, // Thursday: 11 hours (150%)
                5 => 8.0,  // Friday: 8 hours (but weekly total > 40, so some 150%)
                _ => 8.0
            };

            return new PartRide
            {
                Id = Guid.NewGuid(),
                Date = DateTime.SpecifyKind(date, DateTimeKind.Utc),
                Start = new TimeSpan(6, 0, 0),
                End = new TimeSpan(6 + (int)hours, (int)((hours % 1) * 60), 0),
                Rest = new TimeSpan(1, 0, 0),
                TotalKilometers = 350,
                ExtraKilometers = (int)(hours * 8), // More km for longer days
                CarId = carId,
                DriverId = driverId,
                ClientId = clientId,
                CompanyId = companyId,
                WeekNumber = weekNr,
                DecimalHours = hours,
                HoursCodeId = hoursCodeId,
                VacationHours = hours * 0.04, // Earned vacation
                NightAllowance = hours > 10 ? 2.0 * 0.19 : 0, // Some night allowance for long days
                KilometerReimbursement = (int)(hours * 8) * 0.23,
                ConsignmentFee = hours > 10 ? 35.0 : 0,
                TaxFreeCompensation = hours * 1.50,
                VariousCompensation = hours > 12 ? 25.0 : 0,
                Remark = $"SIGNED_WEEK_TEST_DATA - EXTREME OVERTIME ({hours}h), Week {weekNr}, Day {dayInWeek}",
                Status = PartRideStatus.Accepted,
                WeekApprovalId = weekApprovalId
            };
        }

        private static DateTime GetWeekStartDate(int year, int weekNumber)
        {
            var jan1 = new DateTime(year, 1, 1);
            var daysOffset = (int)System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek - (int)jan1.DayOfWeek;
            var firstWeek = jan1.AddDays(daysOffset);
            
            return firstWeek.AddDays((weekNumber - 1) * 7);
        }
    }
}