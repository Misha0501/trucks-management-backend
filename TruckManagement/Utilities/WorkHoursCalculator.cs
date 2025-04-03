namespace TruckManagement;

public class ConsignmentRate
{
    public DateTime Date { get; set; }
    public double Taxed { get; set; }    // 2nd column in Tabel1
    public double Untaxed { get; set; }  // 3rd column in Tabel1
}
public class WorkHoursCalculator
{
    static double dayRateBefore18 = 0.77; // Example placeholders, replace with real config
    static double eveningRateAfter18 = 3.51;
    static string SINGLE_DAY_TRIP_CODE = "One day ride";
    static string SICK_CODE = "Sick";
    static string TIME_FOR_TIME_CODE = "Time for time";
    static string HOLIDAY_CODE = "Holiday";

    // New static fields for departure day calculation
    static string DEPARTURE_CODE = "Multi-day trip departure";
    static string ARRIVAL_CODE = "Multi-day trip arrival"; // new
    static double MULTI_DAY_ALLOWANCE_BEFORE_17H = 1.54;
    static double MULTI_DAY_ALLOWANCE_AFTER_17H = 3.51;
    
    static string INTERMEDIATE_DAY_CODE = "Multi-day trip intermediate day";
    static double MULTI_DAY_ALLOWANCE_INTERMEDIATE = 60.60; // Example placeholder
    static string COURSE_DAY_CODE = "Course day"; // e.g. "C$131"

    
    private static readonly List<ConsignmentRate> RatesTable = new()
    {
        new ConsignmentRate { Date = new DateTime(2024, 1, 1), Taxed = 25.68, Untaxed = 3.21 },
        new ConsignmentRate { Date = new DateTime(2024, 7, 1), Taxed = 26.16, Untaxed = 3.27 },
        new ConsignmentRate { Date = new DateTime(2025, 1, 1), Taxed = 27.20, Untaxed = 3.40 }
    };

    public static double CalculateTotalBreak(
        bool breakScheduleOn,
        double startTime,
        double endTime,
        string hourCode,
        double sickHours,
        double holidayHours
    )
    {
        // 1) In Excel: IF(Admin!E$20="ja", ...) => only proceed if breakScheduleOn == "ja"
        if (!breakScheduleOn)
            return 0.0;

        // 2) If endTime is 0 => same as Excel's "G6=0 or blank"
        if (endTime == 0.0)
            return 0.0;

        // 3) If codeE6 == timeForTimeCode (e.g. "tvt") or (sick + holiday) > 0 => break is 0
        if (hourCode == TIME_FOR_TIME_CODE || (sickHours + holidayHours) > 0)
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

    public static double CalculateHolidayHours(
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

    public static double CalculateSickHours(
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

    public static double CalculateUntaxedAllowanceNormalDayPartial(
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
            totalAllowance = shiftLength * dayRateBefore18;
        }
        else
        {
            // Otherwise, if shift ends at or after 18:00, split rates
            if (endOfShift >= 18.0)
            {
                double hoursBefore18 = 18.0 - startOfShift;
                double hoursAfter18 = endOfShift - 18.0;
                totalAllowance = (hoursBefore18 * dayRateBefore18)
                                 + (hoursAfter18 * eveningRateAfter18);
            }
            else
            {
                // Ends before 18:00 => entire shift at dayRateBefore18
                totalAllowance = shiftLength * dayRateBefore18;
            }
        }

        return Math.Round(totalAllowance, 2);
    }

    public static double CalculateUntaxedAllowanceSingleDay(
        string hourCode, // E6, e.g. "1", "vak", "C123", etc.
        double startTime, // F6
        double endTime, // G6
        double untaxedAllowanceNormalDayPartial, // AG6 result, i.e. same-day partial logic
        double lumpSumIf12h // AC6, TDonbelast for 12+ hours (cross-midnight)
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
                result = hoursBefore18PlusMorning * dayRateBefore18
                         + (6.0 * eveningRateAfter18);
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
                    result = totalShift * dayRateBefore18;
                }
                else
                {
                    // totalShift >= 12 => add AC6 lumpsum
                    result = (totalShift * dayRateBefore18) + lumpSumIf12h;
                }
            }

            return result;
        }
    }

    public static double CalculateTotalHours(
        double shiftStart, // Was F7 in Excel
        double shiftEnd, // Was G7 in Excel
        double breakDuration, // Was H7 in Excel (the break)
        double manualAdjustment // Was I7 in Excel
    )
    {
        double totalHours = (shiftEnd - shiftStart) - breakDuration + manualAdjustment;

        return Math.Round(totalHours, 2);
    }

    public static double CalculateUntaxedAllowanceDepartureDay(
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
                (hoursUntil17 * MULTI_DAY_ALLOWANCE_BEFORE_17H)
                + (7.0 * MULTI_DAY_ALLOWANCE_AFTER_17H),
                2
            );
        }
        else if (departureStartTime >= 17.0)
        {
            // (24 - start)* AE6
            double remainingHours = 24.0 - departureStartTime;
            return Math.Round(
                remainingHours * MULTI_DAY_ALLOWANCE_BEFORE_17H, // "AE6" from formula
                2
            );
        }
        else
        {
            return 0.0;
        }
    }
    public static double CalculateUntaxedAllowanceIntermediateDay(
        string hoursCode // e.g. E6 in your Excel example
    )
    {
        // If hourCode != "Multi-day trip intermediate day" => return 0
        if (hoursCode != INTERMEDIATE_DAY_CODE)
            return 0.0;

        // Otherwise => return the intermediate-day allowance
        return MULTI_DAY_ALLOWANCE_INTERMEDIATE;
    }
    
    public static double CalculateUntaxedAllowanceArrivalDay(
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
            return Math.Round(arrivalEndTime * MULTI_DAY_ALLOWANCE_BEFORE_17H, 2);
        }
        // 2) Else if G6 < 18 => (6 * AD6) + ((G6 - 6) * AE6)
        else if (arrivalEndTime < 18.0)
        {
            double hoursAfter6  = arrivalEndTime - 6.0;
            return Math.Round(
                (6.0 * MULTI_DAY_ALLOWANCE_AFTER_17H)
                + (hoursAfter6 * MULTI_DAY_ALLOWANCE_BEFORE_17H),
                2
            );
        }
        // 3) Else if G6 >= 18 => ((G6 - 18)*AD6) + (12*AE6) + (6*AD6)
        else
        {
            double hoursAfter18 = arrivalEndTime - 18.0;
            return Math.Round(
                (hoursAfter18 * MULTI_DAY_ALLOWANCE_AFTER_17H)
                + (12.0 * MULTI_DAY_ALLOWANCE_BEFORE_17H)
                + (6.0 * MULTI_DAY_ALLOWANCE_AFTER_17H),
                2
            );
        }
    }
    public static double CalculateConsignmentAllowance(
        string hourCode,           // E6
        DateTime dateLookup,       // D6
        double startTime,          // F6
        double endTime            // G6
    )
    {
        // If hourCode != "Consignment" => 0
        if (hourCode != "Consignment")
            return 0.0;

        // 1) VLOOKUP(D6, Tabel1[#All], 3, 1) => approximate match on 'dateLookup'
        //    We'll pick the largest date <= dateLookup (common approach).
        //    If none is <= dateLookup, we might pick the earliest row or return 0.
        var matchedRate = RatesTable
            .Where(r => r.Date <= dateLookup)
            .OrderByDescending(r => r.Date)
            .FirstOrDefault();
    
        if (matchedRate == null)
        {
            // If there's no date less than or equal to dateLookup, you could pick the earliest,
            // or just do 0. We'll do 0 for now.
            return 0.0;
        }
    
        double untaxedValue = matchedRate.Untaxed;

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
    
    public static double CalculateSaturdayHours(
        DateTime date,         // replaces dayName, e.g., 2025-04-05
        string holidayName,    // AM11 in Excel (empty if no holiday)
        string hoursCode,      // E11 in Excel (e.g. "Course day")
        double totalHours      // AT11 in Excel (the total hours to consider)
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
}