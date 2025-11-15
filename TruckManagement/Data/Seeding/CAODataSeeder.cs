using Microsoft.EntityFrameworkCore;
using TruckManagement.Entities;

namespace TruckManagement.Data.Seeding
{
    /// <summary>
    /// Seeder for CAO (Collective Labor Agreement) lookup tables.
    /// Seeds pay scales and vacation days data from the TLN Beroepsgoederenvervoer CAO.
    /// </summary>
    public static class CAODataSeeder
    {
        /// <summary>
        /// Seeds both CAO pay scales and vacation days if they don't already exist.
        /// </summary>
        public static async Task SeedCAODataAsync(ApplicationDbContext context)
        {
            await SeedCAOPayScalesAsync(context);
            await SeedCAOVacationDaysAsync(context);
        }

        /// <summary>
        /// Seeds CAO pay scale data for 2025 if not already seeded.
        /// Data source: TruckManagement/Docs/CAOPayScales_SeedData.csv
        /// </summary>
        private static async Task SeedCAOPayScalesAsync(ApplicationDbContext context)
        {
            // Check if data already exists for 2025
            var existingCount = await context.CAOPayScales
                .CountAsync(ps => ps.EffectiveYear == 2025);

            if (existingCount > 0)
            {
                Console.WriteLine($"CAO Pay Scales already seeded for 2025 ({existingCount} records found). Skipping...");
                return;
            }

            Console.WriteLine("Seeding CAO Pay Scales for 2025...");

            var effectiveFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var payScales = new List<CAOPayScale>
            {
                // Scale A (6 steps)
                new() { Scale = "A", Step = 1, WeeklyWage = 562.40m, FourWeekWage = 2249.60m, MonthlyWage = 2446.44m, HourlyWage100 = 14.06m, HourlyWage130 = 18.28m, HourlyWage150 = 21.09m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "A", Step = 2, WeeklyWage = 569.12m, FourWeekWage = 2276.48m, MonthlyWage = 2474.53m, HourlyWage100 = 14.23m, HourlyWage130 = 18.50m, HourlyWage150 = 21.35m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "A", Step = 3, WeeklyWage = 591.88m, FourWeekWage = 2367.52m, MonthlyWage = 2573.49m, HourlyWage100 = 14.80m, HourlyWage130 = 19.24m, HourlyWage150 = 22.20m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "A", Step = 4, WeeklyWage = 615.55m, FourWeekWage = 2462.20m, MonthlyWage = 2676.41m, HourlyWage100 = 15.39m, HourlyWage130 = 20.01m, HourlyWage150 = 23.09m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "A", Step = 5, WeeklyWage = 640.17m, FourWeekWage = 2560.68m, MonthlyWage = 2783.46m, HourlyWage100 = 16.00m, HourlyWage130 = 20.80m, HourlyWage150 = 24.00m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "A", Step = 6, WeeklyWage = 665.78m, FourWeekWage = 2663.12m, MonthlyWage = 2894.81m, HourlyWage100 = 16.64m, HourlyWage130 = 21.63m, HourlyWage150 = 24.96m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },

                // Scale B (6 steps)
                new() { Scale = "B", Step = 1, WeeklyWage = 576.08m, FourWeekWage = 2304.32m, MonthlyWage = 2504.80m, HourlyWage100 = 14.40m, HourlyWage130 = 18.72m, HourlyWage150 = 21.60m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "B", Step = 2, WeeklyWage = 599.12m, FourWeekWage = 2396.48m, MonthlyWage = 2604.97m, HourlyWage100 = 14.98m, HourlyWage130 = 19.47m, HourlyWage150 = 22.47m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "B", Step = 3, WeeklyWage = 623.08m, FourWeekWage = 2492.32m, MonthlyWage = 2709.15m, HourlyWage100 = 15.58m, HourlyWage130 = 20.25m, HourlyWage150 = 23.37m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "B", Step = 4, WeeklyWage = 648.00m, FourWeekWage = 2592.00m, MonthlyWage = 2817.50m, HourlyWage100 = 16.20m, HourlyWage130 = 21.06m, HourlyWage150 = 24.30m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "B", Step = 5, WeeklyWage = 673.92m, FourWeekWage = 2695.68m, MonthlyWage = 2930.20m, HourlyWage100 = 16.85m, HourlyWage130 = 21.91m, HourlyWage150 = 25.28m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "B", Step = 6, WeeklyWage = 700.88m, FourWeekWage = 2803.52m, MonthlyWage = 3047.43m, HourlyWage100 = 17.52m, HourlyWage130 = 22.78m, HourlyWage150 = 26.28m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },

                // Scale C (6 steps)
                new() { Scale = "C", Step = 1, WeeklyWage = 601.03m, FourWeekWage = 2404.12m, MonthlyWage = 2613.28m, HourlyWage100 = 15.03m, HourlyWage130 = 19.54m, HourlyWage150 = 22.55m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "C", Step = 2, WeeklyWage = 625.07m, FourWeekWage = 2500.28m, MonthlyWage = 2717.80m, HourlyWage100 = 15.63m, HourlyWage130 = 20.32m, HourlyWage150 = 23.45m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "C", Step = 3, WeeklyWage = 650.07m, FourWeekWage = 2600.28m, MonthlyWage = 2826.50m, HourlyWage100 = 16.25m, HourlyWage130 = 21.13m, HourlyWage150 = 24.38m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "C", Step = 4, WeeklyWage = 676.07m, FourWeekWage = 2704.28m, MonthlyWage = 2939.55m, HourlyWage100 = 16.90m, HourlyWage130 = 21.97m, HourlyWage150 = 25.35m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "C", Step = 5, WeeklyWage = 703.11m, FourWeekWage = 2812.44m, MonthlyWage = 3057.12m, HourlyWage100 = 17.58m, HourlyWage130 = 22.85m, HourlyWage150 = 26.37m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "C", Step = 6, WeeklyWage = 731.23m, FourWeekWage = 2924.92m, MonthlyWage = 3179.39m, HourlyWage100 = 18.28m, HourlyWage130 = 23.76m, HourlyWage150 = 27.42m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },

                // Scale D (6 steps)
                new() { Scale = "D", Step = 1, WeeklyWage = 639.89m, FourWeekWage = 2559.56m, MonthlyWage = 2782.24m, HourlyWage100 = 16.00m, HourlyWage130 = 20.80m, HourlyWage150 = 24.00m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "D", Step = 2, WeeklyWage = 665.49m, FourWeekWage = 2661.96m, MonthlyWage = 2893.55m, HourlyWage100 = 16.64m, HourlyWage130 = 21.63m, HourlyWage150 = 24.96m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "D", Step = 3, WeeklyWage = 692.11m, FourWeekWage = 2768.44m, MonthlyWage = 3009.29m, HourlyWage100 = 17.30m, HourlyWage130 = 22.49m, HourlyWage150 = 25.95m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "D", Step = 4, WeeklyWage = 719.79m, FourWeekWage = 2879.16m, MonthlyWage = 3129.65m, HourlyWage100 = 17.99m, HourlyWage130 = 23.39m, HourlyWage150 = 26.99m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "D", Step = 5, WeeklyWage = 748.58m, FourWeekWage = 2994.32m, MonthlyWage = 3254.83m, HourlyWage100 = 18.71m, HourlyWage130 = 24.32m, HourlyWage150 = 28.07m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "D", Step = 6, WeeklyWage = 778.52m, FourWeekWage = 3114.08m, MonthlyWage = 3385.00m, HourlyWage100 = 19.46m, HourlyWage130 = 25.30m, HourlyWage150 = 29.19m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },

                // Scale E (7 steps)
                new() { Scale = "E", Step = 1, WeeklyWage = 671.13m, FourWeekWage = 2684.52m, MonthlyWage = 2918.07m, HourlyWage100 = 16.78m, HourlyWage130 = 21.81m, HourlyWage150 = 25.17m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "E", Step = 2, WeeklyWage = 697.97m, FourWeekWage = 2791.88m, MonthlyWage = 3034.77m, HourlyWage100 = 17.45m, HourlyWage130 = 22.69m, HourlyWage150 = 26.18m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "E", Step = 3, WeeklyWage = 725.89m, FourWeekWage = 2903.56m, MonthlyWage = 3156.17m, HourlyWage100 = 18.15m, HourlyWage130 = 23.60m, HourlyWage150 = 27.23m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "E", Step = 4, WeeklyWage = 754.93m, FourWeekWage = 3019.72m, MonthlyWage = 3282.44m, HourlyWage100 = 18.87m, HourlyWage130 = 24.53m, HourlyWage150 = 28.31m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "E", Step = 5, WeeklyWage = 785.13m, FourWeekWage = 3140.52m, MonthlyWage = 3413.75m, HourlyWage100 = 19.63m, HourlyWage130 = 25.52m, HourlyWage150 = 29.45m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "E", Step = 6, WeeklyWage = 816.54m, FourWeekWage = 3266.16m, MonthlyWage = 3550.32m, HourlyWage100 = 20.41m, HourlyWage130 = 26.53m, HourlyWage150 = 30.62m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "E", Step = 7, WeeklyWage = 849.20m, FourWeekWage = 3396.80m, MonthlyWage = 3692.32m, HourlyWage100 = 21.23m, HourlyWage130 = 27.60m, HourlyWage150 = 31.85m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },

                // Scale F (8 steps)
                new() { Scale = "F", Step = 1, WeeklyWage = 701.43m, FourWeekWage = 2805.72m, MonthlyWage = 3049.82m, HourlyWage100 = 17.54m, HourlyWage130 = 22.80m, HourlyWage150 = 26.31m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "F", Step = 2, WeeklyWage = 729.49m, FourWeekWage = 2917.96m, MonthlyWage = 3171.82m, HourlyWage100 = 18.24m, HourlyWage130 = 23.71m, HourlyWage150 = 27.36m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "F", Step = 3, WeeklyWage = 758.67m, FourWeekWage = 3034.68m, MonthlyWage = 3298.70m, HourlyWage100 = 18.97m, HourlyWage130 = 24.66m, HourlyWage150 = 28.46m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "F", Step = 4, WeeklyWage = 789.02m, FourWeekWage = 3156.08m, MonthlyWage = 3430.66m, HourlyWage100 = 19.73m, HourlyWage130 = 25.65m, HourlyWage150 = 29.60m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "F", Step = 5, WeeklyWage = 820.58m, FourWeekWage = 3282.32m, MonthlyWage = 3567.88m, HourlyWage100 = 20.51m, HourlyWage130 = 26.66m, HourlyWage150 = 30.77m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "F", Step = 6, WeeklyWage = 853.40m, FourWeekWage = 3413.60m, MonthlyWage = 3710.58m, HourlyWage100 = 21.34m, HourlyWage130 = 27.74m, HourlyWage150 = 32.01m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "F", Step = 7, WeeklyWage = 887.54m, FourWeekWage = 3550.16m, MonthlyWage = 3859.02m, HourlyWage100 = 22.19m, HourlyWage130 = 28.85m, HourlyWage150 = 33.29m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "F", Step = 8, WeeklyWage = 923.04m, FourWeekWage = 3692.16m, MonthlyWage = 4013.38m, HourlyWage100 = 23.08m, HourlyWage130 = 30.00m, HourlyWage150 = 34.62m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },

                // Scale G (9 steps)
                new() { Scale = "G", Step = 1, WeeklyWage = 741.01m, FourWeekWage = 2964.04m, MonthlyWage = 3221.91m, HourlyWage100 = 18.53m, HourlyWage130 = 24.09m, HourlyWage150 = 27.80m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "G", Step = 2, WeeklyWage = 770.65m, FourWeekWage = 3082.60m, MonthlyWage = 3350.79m, HourlyWage100 = 19.27m, HourlyWage130 = 25.05m, HourlyWage150 = 28.91m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "G", Step = 3, WeeklyWage = 801.48m, FourWeekWage = 3205.92m, MonthlyWage = 3484.84m, HourlyWage100 = 20.04m, HourlyWage130 = 26.05m, HourlyWage150 = 30.06m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "G", Step = 4, WeeklyWage = 833.54m, FourWeekWage = 3334.16m, MonthlyWage = 3624.23m, HourlyWage100 = 20.84m, HourlyWage130 = 27.09m, HourlyWage150 = 31.26m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "G", Step = 5, WeeklyWage = 866.88m, FourWeekWage = 3467.52m, MonthlyWage = 3769.19m, HourlyWage100 = 21.67m, HourlyWage130 = 28.17m, HourlyWage150 = 32.51m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "G", Step = 6, WeeklyWage = 901.55m, FourWeekWage = 3606.20m, MonthlyWage = 3919.94m, HourlyWage100 = 22.54m, HourlyWage130 = 29.30m, HourlyWage150 = 33.81m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "G", Step = 7, WeeklyWage = 937.61m, FourWeekWage = 3750.44m, MonthlyWage = 4076.73m, HourlyWage100 = 23.44m, HourlyWage130 = 30.47m, HourlyWage150 = 35.16m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "G", Step = 8, WeeklyWage = 975.11m, FourWeekWage = 3900.44m, MonthlyWage = 4239.78m, HourlyWage100 = 24.38m, HourlyWage130 = 31.69m, HourlyWage150 = 36.57m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "G", Step = 9, WeeklyWage = 1014.11m, FourWeekWage = 4056.44m, MonthlyWage = 4409.35m, HourlyWage100 = 25.35m, HourlyWage130 = 32.96m, HourlyWage150 = 38.03m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },

                // Scale H (10 steps)
                new() { Scale = "H", Step = 1, WeeklyWage = 780.73m, FourWeekWage = 3122.92m, MonthlyWage = 3394.61m, HourlyWage100 = 19.52m, HourlyWage130 = 25.38m, HourlyWage150 = 29.28m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "H", Step = 2, WeeklyWage = 811.96m, FourWeekWage = 3247.84m, MonthlyWage = 3530.40m, HourlyWage100 = 20.30m, HourlyWage130 = 26.39m, HourlyWage150 = 30.45m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "H", Step = 3, WeeklyWage = 844.44m, FourWeekWage = 3377.76m, MonthlyWage = 3671.63m, HourlyWage100 = 21.11m, HourlyWage130 = 27.44m, HourlyWage150 = 31.67m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "H", Step = 4, WeeklyWage = 878.22m, FourWeekWage = 3512.88m, MonthlyWage = 3818.50m, HourlyWage100 = 21.96m, HourlyWage130 = 28.55m, HourlyWage150 = 32.94m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "H", Step = 5, WeeklyWage = 913.35m, FourWeekWage = 3653.40m, MonthlyWage = 3971.25m, HourlyWage100 = 22.83m, HourlyWage130 = 29.68m, HourlyWage150 = 34.25m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "H", Step = 6, WeeklyWage = 949.88m, FourWeekWage = 3799.52m, MonthlyWage = 4130.08m, HourlyWage100 = 23.75m, HourlyWage130 = 30.88m, HourlyWage150 = 35.63m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "H", Step = 7, WeeklyWage = 987.87m, FourWeekWage = 3951.48m, MonthlyWage = 4295.26m, HourlyWage100 = 24.70m, HourlyWage130 = 32.11m, HourlyWage150 = 37.05m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "H", Step = 8, WeeklyWage = 1027.38m, FourWeekWage = 4109.52m, MonthlyWage = 4467.05m, HourlyWage100 = 25.68m, HourlyWage130 = 33.38m, HourlyWage150 = 38.52m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "H", Step = 9, WeeklyWage = 1068.48m, FourWeekWage = 4273.92m, MonthlyWage = 4645.75m, HourlyWage100 = 26.71m, HourlyWage130 = 34.72m, HourlyWage150 = 40.07m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { Scale = "H", Step = 10, WeeklyWage = 1111.22m, FourWeekWage = 4444.88m, MonthlyWage = 4831.58m, HourlyWage100 = 27.78m, HourlyWage130 = 36.11m, HourlyWage150 = 41.67m, EffectiveYear = 2025, EffectiveFrom = effectiveFrom }
            };

            context.CAOPayScales.AddRange(payScales);
            await context.SaveChangesAsync();

            Console.WriteLine($"✅ Successfully seeded {payScales.Count} CAO pay scale records for 2025.");
        }

        /// <summary>
        /// Seeds CAO vacation days data for 2025 if not already seeded.
        /// Data source: TruckManagement/Docs/CAOVacationDays_SeedData.csv
        /// </summary>
        private static async Task SeedCAOVacationDaysAsync(ApplicationDbContext context)
        {
            // Check if data already exists for 2025
            var existingCount = await context.CAOVacationDays
                .CountAsync(vd => vd.EffectiveYear == 2025);

            if (existingCount > 0)
            {
                Console.WriteLine($"CAO Vacation Days already seeded for 2025 ({existingCount} records found). Skipping...");
                return;
            }

            Console.WriteLine("Seeding CAO Vacation Days for 2025...");

            var effectiveFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var vacationDays = new List<CAOVacationDays>
            {
                new() { AgeFrom = 0, AgeTo = 16, AgeGroupDescription = "< 16 jaar", VacationDays = 22, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { AgeFrom = 17, AgeTo = 18, AgeGroupDescription = "17 en 18 jaar", VacationDays = 23, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { AgeFrom = 19, AgeTo = 39, AgeGroupDescription = "19 t/m 39 jaar", VacationDays = 24, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { AgeFrom = 40, AgeTo = 44, AgeGroupDescription = "40 t/m 44 jaar", VacationDays = 24, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { AgeFrom = 45, AgeTo = 49, AgeGroupDescription = "45 t/m 49 jaar", VacationDays = 25, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { AgeFrom = 50, AgeTo = 54, AgeGroupDescription = "50 t/m 54 jaar", VacationDays = 26, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { AgeFrom = 55, AgeTo = 59, AgeGroupDescription = "55 t/m 59 jaar", VacationDays = 27, EffectiveYear = 2025, EffectiveFrom = effectiveFrom },
                new() { AgeFrom = 60, AgeTo = 140, AgeGroupDescription = "> 60 jaar", VacationDays = 28, EffectiveYear = 2025, EffectiveFrom = effectiveFrom }
            };

            context.CAOVacationDays.AddRange(vacationDays);
            await context.SaveChangesAsync();

            Console.WriteLine($"✅ Successfully seeded {vacationDays.Count} CAO vacation days records for 2025.");
        }
    }
}

