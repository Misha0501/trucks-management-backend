using TruckManagement.Entities;

namespace TruckManagement.Data.Seeding;

public static class CaoSeeder
{
    public static async Task SeedAsync(ApplicationDbContext dbContext)
    {
        if (dbContext.Caos.Any())
            return;

        var nightStart = new TimeSpan(21, 0, 0);
        var nightEnd = new TimeSpan(5, 0, 0);

        var caoEntries = new List<Cao>
        {
            // 1) 01/01/2021 -> 30/06/2021
            new Cao
            {
                Id = 1,
                StartDate = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2021, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                StandardUntaxedAllowance = 0.63m,
                MultiDayAfter17Allowance = 2.90m,
                MultiDayBefore17Allowance = 1.28m,
                ShiftMoreThan12HAllowance = 12.10m,
                MultiDayTaxedAllowance = 21.40m,
                MultiDayUntaxedAllowance = 50.16m,
                ConsignmentUntaxedAllowance = 2.69m,
                ConsignmentTaxedAllowance = 25.68m,
                CommuteMinKilometers = 10, 
                CommuteMaxKilometers = 35, 
                KilometersAllowance = 0.00m, 
                NightHoursAllowanceRate = 0.19m,
                NightTimeStart = nightStart,
                NightTimeEnd = nightEnd
            },
            // 2) 01/07/2021 -> 31/12/2021
            new Cao
            {
                Id = 2,
                StartDate = new DateTime(2021, 7, 1,0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2021, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                StandardUntaxedAllowance = 0.65m,
                MultiDayAfter17Allowance = 2.98m,
                MultiDayBefore17Allowance = 1.31m,
                ShiftMoreThan12HAllowance = 12.43m,
                MultiDayTaxedAllowance = 22.15m,
                MultiDayUntaxedAllowance = 51.48m,
                ConsignmentUntaxedAllowance = 2.78m,
                ConsignmentTaxedAllowance = 25.68m,
                CommuteMinKilometers = 10,
                CommuteMaxKilometers = 35,
                KilometersAllowance = 0.00m,
                NightHoursAllowanceRate = 0.19m,
                NightTimeStart = nightStart,
                NightTimeEnd = nightEnd
            },
            // 3) 01/01/2022 -> 31/12/2022
            new Cao
            {
                Id = 3,
                StartDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2022, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                StandardUntaxedAllowance = 0.67m,
                MultiDayAfter17Allowance = 3.08m,
                MultiDayBefore17Allowance = 1.35m,
                ShiftMoreThan12HAllowance = 12.83m,
                MultiDayTaxedAllowance = 22.87m,
                MultiDayUntaxedAllowance = 53.16m,
                ConsignmentUntaxedAllowance = 2.87m,
                ConsignmentTaxedAllowance = 25.68m,
                CommuteMinKilometers = 10,
                CommuteMaxKilometers = 35,
                KilometersAllowance = 0.00m,
                NightHoursAllowanceRate = 0.19m,
                NightTimeStart = nightStart,
                NightTimeEnd = nightEnd
            },
            // 4) 01/01/2023 -> 31/12/2023
            new Cao
            {
                Id = 4,
                StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                StandardUntaxedAllowance = 0.72m,
                MultiDayAfter17Allowance = 3.31m,
                MultiDayBefore17Allowance = 1.45m,
                ShiftMoreThan12HAllowance = 13.79m,
                MultiDayTaxedAllowance = 24.59m,
                MultiDayUntaxedAllowance = 57.12m,
                ConsignmentUntaxedAllowance = 3.09m,
                ConsignmentTaxedAllowance = 25.68m,
                CommuteMinKilometers = 10,
                CommuteMaxKilometers = 35,
                KilometersAllowance = 0.21m,
                NightHoursAllowanceRate = 0.19m,
                NightTimeStart = nightStart,
                NightTimeEnd = nightEnd
            },
            // 5) 01/01/2024 -> 30/06/2024
            new Cao
            {
                Id = 5,
                StartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2024, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                StandardUntaxedAllowance = 0.75m,
                MultiDayAfter17Allowance = 3.44m,
                MultiDayBefore17Allowance = 1.51m,
                ShiftMoreThan12HAllowance = 14.34m,
                MultiDayTaxedAllowance = 25.57m,
                MultiDayUntaxedAllowance = 59.40m,
                ConsignmentUntaxedAllowance = 3.21m,
                ConsignmentTaxedAllowance = 25.68m,
                CommuteMinKilometers = 10,
                CommuteMaxKilometers = 35,
                KilometersAllowance = 0.23m,
                NightHoursAllowanceRate = 0.19m,
                NightTimeStart = nightStart,
                NightTimeEnd = nightEnd
            },
            // 6) 01/07/2024 -> 31/12/2024
            new Cao
            {
                Id = 6,
                StartDate = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                StandardUntaxedAllowance = 0.77m,
                MultiDayAfter17Allowance = 3.51m,
                MultiDayBefore17Allowance = 1.54m,
                ShiftMoreThan12HAllowance = 14.63m,
                MultiDayTaxedAllowance = 26.08m,
                MultiDayUntaxedAllowance = 60.60m,
                ConsignmentUntaxedAllowance = 3.27m,
                ConsignmentTaxedAllowance = 26.16m,
                CommuteMinKilometers = 10,
                CommuteMaxKilometers = 35,
                KilometersAllowance = 0.23m,
                NightHoursAllowanceRate = 0.19m,
                NightTimeStart = nightStart,
                NightTimeEnd = nightEnd
            },
            // 7) 01/01/2025 -> far future
            new Cao
            {
                Id = 7,
                StartDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2099, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                StandardUntaxedAllowance = 0.80m,
                MultiDayAfter17Allowance = 3.65m,
                MultiDayBefore17Allowance = 1.60m,
                ShiftMoreThan12HAllowance = 15.22m,
                MultiDayTaxedAllowance = 27.12m,
                MultiDayUntaxedAllowance = 63.00m,
                ConsignmentUntaxedAllowance = 3.40m,
                ConsignmentTaxedAllowance = 27.20m,
                CommuteMinKilometers = 10,
                CommuteMaxKilometers = 35,
                KilometersAllowance = 0.23m,
                NightHoursAllowanceRate = 0.19m,
                NightTimeStart = nightStart,
                NightTimeEnd = nightEnd
            },
        };

        dbContext.Caos.AddRange(caoEntries);
        await dbContext.SaveChangesAsync();
    }
}