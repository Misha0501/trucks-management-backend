using TruckManagement.Entities;

namespace TruckManagement.Data.Seeding;

public static class VacationRightSeeder
{
    public static async Task SeedAsync(ApplicationDbContext dbContext)
    {
        if (dbContext.VacationRights.Any())
            return;

        var entries = new List<VacationRight>
        {
            // 1) 01/01/2021 -> 30/06/2021
            new VacationRight
            {
                Id = 1,
                AgeFrom = null,
                AgeTo = 16,
                Description = "Younger than 16 years",
                Right = 22,
                StartDate = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = null
            },
            new VacationRight
            {
                Id = 2,
                AgeFrom = 17,
                AgeTo = 18,
                Description = "17 and 18 years",
                Right = 23,
                StartDate = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = null
            },
            new VacationRight
            {
                Id = 3,
                AgeFrom = 19,
                AgeTo = 39,
                Description = "19 to 39 years",
                Right = 24,
                StartDate = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = null
            },
            new VacationRight
            {
                Id = 4,
                AgeFrom = 40,
                AgeTo = 44,
                Description = "40 to 44 years",
                Right = 24,
                StartDate = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = null
            },
            new VacationRight
            {
                Id = 5,
                AgeFrom = 45,
                AgeTo = 49,
                Description = "45 to 49 years",
                Right = 25,
                StartDate = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = null
            },
            new VacationRight
            {
                Id = 6,
                AgeFrom = 50,
                AgeTo = 54,
                Description = "50 to 54 years",
                Right = 26,
                StartDate = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = null
            },
            new VacationRight
            {
                Id = 7,
                AgeFrom = 55,
                AgeTo = 59,
                Description = "55 to 59 years",
                Right = 27,
                StartDate = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = null
            },
            new VacationRight
            {
                Id = 8,
                AgeFrom = 60,
                AgeTo = 140,
                Description = "60 years and older",
                Right = 28,
                StartDate = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = null
            }
        };

        dbContext.VacationRights.AddRange(entries);
        await dbContext.SaveChangesAsync();
    }
}