using TruckManagement.Entities;

namespace TruckManagement;

public class WorkHoursCalculator
{
    private readonly Cao _cao; // The CAO row for this date
    static string SINGLE_DAY_TRIP_CODE = "One day ride";
    static string SICK_CODE = "Sick";
    static string TIME_FOR_TIME_CODE = "Time for time";
    static string HOLIDAY_CODE = "Holiday";
    static string DEPARTURE_CODE = "Multi-day trip departure";
    static string ARRIVAL_CODE = "Multi-day trip arrival"; // new
    static string INTERMEDIATE_DAY_CODE = "Multi-day trip intermediate day";
    static string COURSE_DAY_CODE = "Course day";
    static string CONSIGNMENT_CODE = "Consignment";
    static string STANDOVER_OPTION = "StandOver";

    private static readonly Dictionary<DateTime, string> DutchHolidays = new()
    {
        { new DateTime(2022, 1, 1), "Nieuwjaarsdag" },
        { new DateTime(2022, 4, 18), "2e Paasdag" },
        { new DateTime(2022, 4, 27), "Koningsdag" },
        { new DateTime(2022, 5, 26), "Hemelvaart" },
        { new DateTime(2022, 6, 6), "2e Pinksterdag" },
        { new DateTime(2022, 12, 26), "2e Kerstdag" },

        { new DateTime(2023, 1, 1), "Nieuwjaarsdag" },
        { new DateTime(2023, 4, 9), "2e Paasdag" },
        { new DateTime(2023, 4, 27), "Koningsdag" },
        { new DateTime(2023, 5, 18), "Hemelvaart" },
        { new DateTime(2023, 12, 25), "1e kerstdag" },
        { new DateTime(2023, 12, 26), "2e Kerstdag" },

        { new DateTime(2024, 1, 1), "Nieuwjaarsdag" },
        { new DateTime(2024, 4, 1), "2e Paasdag" },
        { new DateTime(2024, 4, 27), "Koningsdag" },
        { new DateTime(2024, 5, 9), "Hemelvaart" },
        { new DateTime(2024, 5, 20), "2e Pinksterdag" },
        { new DateTime(2024, 12, 26), "2e Kerstdag" },

        { new DateTime(2025, 1, 1), "Nieuwjaarsdag" },
        { new DateTime(2025, 4, 21), "2e Paasdag" },
        { new DateTime(2025, 4, 26), "Koningsdag" },
        { new DateTime(2025, 5, 5), "bevrijdingsdag" },
        { new DateTime(2025, 5, 29), "Hemelvaart" },
        { new DateTime(2025, 6, 9), "2e Pinksterdag" },
        { new DateTime(2025, 12, 26), "2e Kerstdag" },

        { new DateTime(2026, 1, 1), "Nieuwjaarsdag" },
        { new DateTime(2026, 4, 6), "2e Paasdag" },
        { new DateTime(2026, 4, 27), "Koningsdag" },
        { new DateTime(2026, 5, 14), "Hemelvaart" },
        { new DateTime(2026, 5, 25), "2e Pinksterdag" },
        { new DateTime(2026, 12, 26), "2e Kerstdag" },

        { new DateTime(2027, 1, 1), "Nieuwjaarsdag" },
        { new DateTime(2027, 3, 29), "2e Paasdag" },
        { new DateTime(2027, 4, 27), "Koningsdag" },
        { new DateTime(2027, 6, 6), "Hemelvaart" },
        { new DateTime(2027, 12, 26), "2e Kerstdag" },

        { new DateTime(2028, 1, 1), "Nieuwjaarsdag" },
        { new DateTime(2028, 4, 17), "2e Paasdag" },
        { new DateTime(2028, 4, 27), "Koningsdag" },
        { new DateTime(2028, 5, 25), "Hemelvaart" },
        { new DateTime(2028, 6, 5), "2e Pinksterdag" },
        { new DateTime(2028, 12, 26), "2e Kerstdag" }
    };

    public WorkHoursCalculator(Cao cao)
    {
        _cao = cao ?? throw new ArgumentNullException(nameof(cao));
    }
    
    public double CalculateTotalBreak(
        bool breakScheduleOn,
        double startTime,
        double endTime,
        string hourCode,
        double sickHours,
        double vacationHours
    )
    {
        // 1) In Excel: IF(Admin!E$20="ja", ...) => only proceed if breakScheduleOn == "ja"
        if (!breakScheduleOn)
            return 0.0;

        // 2) If endTime is 0 => same as Excel's "G6=0 or blank"
        if (endTime == 0.0)
            return 0.0;

        // 3) If codeE6 == timeForTimeCode (e.g. "tvt") or (sick + holiday) > 0 => break is 0
        if (hourCode == TIME_FOR_TIME_CODE || (sickHours + vacationHours) > 0)
            return 0.0;

        // 4) Determine time difference across midnight if needed
        double difference;
        if (endTime < startTime)
        {
            difference = 24.0 - startTime + endTime;
        }
        else
        {
            difference = endTime - startTime;
        }

        // 5) Return the break value based on the bracket
        //   Excel logic: >=4.5 & <7.5 => 0.5, >=7.5 & <10.5 => 1, etc.
        if (difference >= 4.5 && difference < 7.5)
            return 0.5;
        else if (difference >= 7.5 && difference < 10.5)
            return 1.0;
        else if (difference >= 10.5 && difference < 13.5)
            return 1.5;
        else if (difference >= 13.5 && difference < 16.5)
            return 2.0;
        else if (difference >= 16.5)
            return 2.5;
        else
            return 0.0;
    }

    public double CalculateVacationHours(
        string hourCode, // The code in E6 (e.g. "1","2","vak","zie","C127", etc.)
        double weeklyPercentage, // e.g., 100 for full-time, 50 for half-time, etc. (cell G2)
        double startTime, // The start time (was F6)
        double endTime // The end time (was G6)
    )
    {
        // 1) Calculate the shift length, handling a possible midnight crossover:
        double shiftHours;
        if (endTime < startTime)
        {
            // crosses midnight
            shiftHours = 24.0 - startTime + endTime;
        }
        else
        {
            // same-day
            shiftHours = endTime - startTime;
        }

        // 2) If the hour code matches the holiday code, count all shift hours as holiday.
        //    Multiply by weeklyPercentage/100 to account for part-time scenarios.
        if (hourCode == HOLIDAY_CODE)
        {
            return shiftHours * (weeklyPercentage / 100.0);
        }
        else
        {
            // Not a holiday code => no holiday hours
            return 0.0;
        }
    }

    public double CalculateSickHours(
        string hourCode, // E6: e.g. "1", "2", "vak", "zie", "C128", "tvt", etc.
        string holidayName, // AM6: the name of a holiday (empty if not a holiday)
        double weeklyPercentage, // G2: e.g. 100 for full-time, 50 for half-time, etc.
        double startTime, // Start time of the shift
        double endTime // End time of the shift
    )
    {
        // 1) Compute the length of this shift in hours, handling midnight crossover:
        double shiftHours;
        if (endTime < startTime)
        {
            // crosses midnight
            shiftHours = 24.0 - startTime + endTime;
        }
        else
        {
            // same-day
            shiftHours = endTime - startTime;
        }

        // 2) If code is "sick" => count these hours as "sick hours"
        //    Or if there's a non-empty holiday name => treat as holiday hours.
        //    (We multiply by weeklyPercentage/100.0 to scale for part-time workers.)

        double scaledHours = shiftHours * (weeklyPercentage / 100.0);

        if (hourCode == SICK_CODE)
        {
            // E6 code matches the "sick" code => fill AP with sick hours
            return scaledHours;
        }
        else if (!string.IsNullOrWhiteSpace(holidayName))
        {
            // AM6 has a holiday name => treat these as holiday hours
            return scaledHours;
        }
        else
        {
            // Neither sick nor holiday => no hours go into AP
            return 0.0;
        }
    }

    public double CalculateUntaxedAllowanceNormalDayPartial(
        double startOfShift, // F6 in Excel
        double endOfShift, // G6 in Excel
        bool isHoliday // true if it's a holiday (AM has a holiday name)
    )
    {
        // 1) If it's a holiday => no untaxed allowance
        if (isHoliday)
            return 0.0;

        // 2) If shift crosses midnight => 0 here, presumably handled elsewhere
        if (endOfShift < startOfShift)
            return 0.0;

        // 3) Calculate total shift length
        double shiftLength = endOfShift - startOfShift;

        // 4) Main logic from AG6 for same-day shift
        double totalAllowance;

        // If shift starts at or after 14:00, everything uses dayRateBefore18
        if (startOfShift >= 14.0)
        {
            totalAllowance = shiftLength * (double)_cao.StandardUntaxedAllowance;
        }
        else
        {
            // Otherwise, if shift ends at or after 18:00, split rates
            if (endOfShift >= 18.0)
            {
                double hoursBefore18 = 18.0 - startOfShift;
                double hoursAfter18 = endOfShift - 18.0;
                totalAllowance = (hoursBefore18 * (double)_cao.StandardUntaxedAllowance)
                                 + (hoursAfter18 * (double)_cao.MultiDayAfter17Allowance);
            }
            else
            {
                // Ends before 18:00 => entire shift at dayRateBefore18
                totalAllowance = shiftLength * (double)_cao.StandardUntaxedAllowance;
            }
        }

        return Math.Round(totalAllowance, 2);
    }

    public double CalculateUntaxedAllowanceSingleDay(
        string hourCode, // E6, e.g. "1", "vak", "C123", etc.
        double startTime, // F6
        double endTime, // G6
        double untaxedAllowanceNormalDayPartial // AG6 result, i.e. same-day partial logic
    )
    {
        // 1) If the code is not "Eendaagserit", Excel returns "" => we do 0
        if (hourCode != SINGLE_DAY_TRIP_CODE)
            return 0.0;

        // 2) If start+end = 0 => no times => 0
        if ((startTime + endTime) == 0.0)
            return 0.0;

        // 3) Check if crossing midnight
        if (endTime > startTime)
        {
            // Same-day scenario => if shift < 4 => 0, else => use the partial normal-day result (AG6)
            double shiftLength = endTime - startTime;
            if (shiftLength < 4.0)
                return 0.0;
            else
                return untaxedAllowanceNormalDayPartial;
        }
        else
        {
            // 4) Cross-midnight scenario (endTime <= startTime)
            //    - If F6 < 14 => ((18 - F6) + G6)*AB6 + (6 * AD6)
            //    - Else check total shift (24 - F6 + G6).
            double result = 0.0;

            if (startTime < 14.0)
            {
                // ((18 - F6) + G6)*AB6 + (6*AD6)
                double hoursBefore18PlusMorning = (18.0 - startTime) + endTime;
                result = hoursBefore18PlusMorning * (double)_cao.StandardUntaxedAllowance
                         + (6.0 * (double)_cao.MultiDayAfter17Allowance);
            }
            else
            {
                // F6 >= 14 => we do (24 - F6 + G6) => X
                double totalShift = (24.0 - startTime) + endTime;
                if (totalShift < 4.0)
                {
                    result = 0.0;
                }
                else if (totalShift < 12.0)
                {
                    result = totalShift * (double)_cao.StandardUntaxedAllowance;
                }
                else
                {
                    // totalShift >= 12 => add AC6 lumpsum
                    result = (totalShift * (double)_cao.StandardUntaxedAllowance) + (double)_cao.ShiftMoreThan12HAllowance;
                }
            }

            return result;
        }
    }

    public double CalculateTotalHours(
        double shiftStart, // Was F7 in Excel
        double shiftEnd, // Was G7 in Excel
        double breakDuration, // Was H7 in Excel (the break)
        double manualAdjustment // Was I7 in Excel
    )
    {
        double totalHours = (shiftEnd - shiftStart) - breakDuration + manualAdjustment;

        return Math.Round(totalHours, 2);
    }

    public double CalculateUntaxedAllowanceDepartureDay(
        string hourCode,
        double departureStartTime)
    {
        // If code isn't "Multi-day trip departure", return 0
        if (hourCode != DEPARTURE_CODE)
            return 0.0;

        // If invalid start time => 0
        if (departureStartTime < 0.0 || departureStartTime >= 24.0)
            return 0.0;

        // AE6 => the “before” rate, AD6 => the “after” rate
        // but from the formula, if start >= 17 => uses AE6 anyway

        if (departureStartTime < 17.0)
        {
            // (17 - start)* AE6 + 7* AD6
            double hoursUntil17 = 17.0 - departureStartTime;
            return Math.Round(
                (hoursUntil17 * (double)_cao.MultiDayBefore17Allowance)
                + (7.0 * (double)_cao.MultiDayAfter17Allowance),
                2
            );
        }
        else if (departureStartTime >= 17.0)
        {
            // (24 - start)* AE6
            double remainingHours = 24.0 - departureStartTime;
            return Math.Round(
                remainingHours * (double)_cao.MultiDayBefore17Allowance, // "AE6" from formula
                2
            );
        }
        else
        {
            return 0.0;
        }
    }

    public double CalculateUntaxedAllowanceIntermediateDay(
        string hourCode,
        string? hourOption,
        double startTime,
        double endTime
    )
    {
        // Must be "Multi-day trip intermediate day"
        if (hourCode != INTERMEDIATE_DAY_CODE)
            return 0.0;

        // If "StandOver" option and both times are 0, check the date-based untaxed rates
        if (hourOption == STANDOVER_OPTION && startTime == 0.0 && endTime == 0.0)
        {
            // Look for the latest applicable rate before or on the given date
            var rate = (double)_cao.StandOverIntermediateDayUntaxed;

            return Math.Round(rate, 2);
        }
        
        // Use MultiDayTripIntermediateUntaxed rates by date
        var defaultRate = (double)_cao.MultiDayUntaxedAllowance;

        return Math.Round(defaultRate, 2);
    }

    public double CalculateUntaxedAllowanceArrivalDay(
        string hourCode, // E6
        double arrivalEndTime // G6
    )
    {
        // If not "Multi-day trip arrival" => return 0
        if (hourCode != ARRIVAL_CODE)
            return 0.0;

        // Validate input
        if (arrivalEndTime < 0.0 || arrivalEndTime > 24.0)
            return 0.0;

        // 1) If G6 <= 12 => G6 * AE6
        if (arrivalEndTime <= 12.0)
        {
            return Math.Round(arrivalEndTime * (double)_cao.MultiDayBefore17Allowance, 2);
        }
        // 2) Else if G6 < 18 => (6 * AD6) + ((G6 - 6) * AE6)
        else if (arrivalEndTime < 18.0)
        {
            double hoursAfter6 = arrivalEndTime - 6.0;
            return Math.Round(
                (6.0 * (double)_cao.MultiDayAfter17Allowance)
                + (hoursAfter6 * (double)_cao.MultiDayBefore17Allowance),
                2
            );
        }
        // 3) Else if G6 >= 18 => ((G6 - 18)*AD6) + (12*AE6) + (6*AD6)
        else
        {
            double hoursAfter18 = arrivalEndTime - 18.0;
            return Math.Round(
                (hoursAfter18 * (double)_cao.MultiDayAfter17Allowance)
                + (12.0 * (double)_cao.MultiDayBefore17Allowance)
                + (6.0 * (double)_cao.MultiDayAfter17Allowance),
                2
            );
        }
    }

    public double CalculateConsignmentAllowance(
        string hourCode, // E6
        DateTime dateLookup, // D6
        double startTime, // F6
        double endTime // G6
    )
    {
        // If hourCode != "Consignment" => 0
        if (hourCode != CONSIGNMENT_CODE)
            return 0.0;

        // 1) VLOOKUP(D6, Tabel1[#All], 3, 1) => approximate match on 'dateLookup'
        //    We'll pick the largest date <= dateLookup (common approach).
        //    If none is <= dateLookup, we might pick the earliest row or return 0.
        double untaxedValue = (double)_cao.ConsignmentUntaxedAllowance;

        // 2) The SHIFT formula: 
        //    If G6 < F6 => shift = 24 - F6 + G6
        //    else => shift = G6 - F6
        //    Then clamp shift between 0 and 8
        double rawShift;
        if (endTime < startTime)
            rawShift = 24.0 - startTime + endTime;
        else
            rawShift = endTime - startTime;

        // Force min 0, then max 8
        rawShift = Math.Max(0.0, rawShift);
        rawShift = Math.Min(8.0, rawShift);

        // 3) Multiply shift by the "onbelast" value
        double result = untaxedValue * rawShift;

        return Math.Round(result, 2);
    }

    public double CalculateSaturdayHours(
        DateTime date, // replaces dayName, e.g., 2025-04-05
        string holidayName, // AM11 in Excel (empty if no holiday)
        string hoursCode, // E11 in Excel (e.g. "Course day")
        double totalHours // AT11 in Excel (the total hours to consider)
    )
    {
        // 1) Check if the date is a Saturday
        if (date.DayOfWeek != DayOfWeek.Saturday)
            return 0.0;

        // 2) Check if it's a holiday
        if (!string.IsNullOrWhiteSpace(holidayName))
            return 0.0;

        // 3) Check if the hour code is "Course day"
        if (hoursCode == COURSE_DAY_CODE)
            return 0.0;

        // All conditions passed, return total hours
        return totalHours;
    }

    public string GetHolidayName(DateTime date, string? hoursOptionName)
    {
        // If the selected hours option explicitly marks the day as a holiday
        if (!string.IsNullOrWhiteSpace(hoursOptionName) && hoursOptionName == "Holiday")
        {
            return "Holiday";
        }


        // If the hours option explicitly says "NoHoliday"
        if (!string.IsNullOrWhiteSpace(hoursOptionName) && hoursOptionName == "NoHoliday")
        {
            return string.Empty;
        }

        // Otherwise, check if the date is a public holiday from the list
        if (DutchHolidays.TryGetValue(date.Date, out var holidayName))
        {
            return holidayName;
        }

        return string.Empty;
    }

    public double CalculateSundayHolidayHours(
        string hourCode,
        DateTime date,
        string? holidayName,
        double totalHours
    )
    {
        // If hour code is "Course day", do not count hours as Sunday hours
        if (hourCode == COURSE_DAY_CODE)
            return 0.0;

        // If it's a holiday, count total hours
        if (!string.IsNullOrWhiteSpace(holidayName))
            return totalHours;

        // If it's a Sunday, count total hours
        if (date.DayOfWeek == DayOfWeek.Sunday)
            return totalHours;

        // Not a Sunday and not a holiday => 0
        return 0.0;
    }

    public double CalculateNetHours(
        string hourCode,
        DateTime day,
        bool isHoliday,
        double totalHours,
        double weeklyPercentage
    )
    {
        // 1) If hourCode is "Consignment", return 0
        if (hourCode == CONSIGNMENT_CODE)
        {
            return Math.Round(0.0, 2);
        }

        // 2) If not Saturday or Sunday and isHoliday == true and totalHours <= 8
        bool isWeekend = day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday;
        if (!isWeekend && isHoliday && totalHours <= 8.0)
        {
            return Math.Round(8.0 * (weeklyPercentage / 100.0), 2);
        }

        // 3) If hourCode is "Holiday", "Sick", or "Time for time"
        bool isNonWorkingHour = hourCode == HOLIDAY_CODE || hourCode == SICK_CODE || hourCode == TIME_FOR_TIME_CODE;
        if (isNonWorkingHour)
        {
            if (totalHours > 0.0)
                return Math.Round(totalHours, 2);
            else
                return Math.Round(8.0 * (weeklyPercentage / 100.0), 2);
        }

        // 4) Default: return the totalHours
        return Math.Round(totalHours, 2);
    }
}