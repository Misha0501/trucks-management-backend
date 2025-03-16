namespace TruckManagement;

public class WorkHoursCalculator
{
        public static double CalculateTotalBreak(
        bool breakScheduleOn,
        double startTime,
        double endTime,
        string hourCode,
        string timeForTimeCode,
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
        if (hourCode == timeForTimeCode || (sickHours + holidayHours) > 0)
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
        string hourCode,        // The code in E6 (e.g. "1","2","vak","zie","C127", etc.)
        double weeklyPercentage,// e.g., 100 for full-time, 50 for half-time, etc. (cell G2)
        double startTime,       // The start time (was F6)
        double endTime          // The end time (was G6)
    )
    {
        string HOLIDAY_CODE = "vak";
        
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
        string SICK_CODE = "zie";
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
        double startOfShift,       // F6 in Excel
        double endOfShift,         // G6 in Excel
        double dayRateBefore18,    // AB6 in Excel (onbelaste vergoeding multiplier for before 18:00)
        double eveningRateAfter18, // AD6 in Excel (onbelaste vergoeding multiplier for after 18:00)
        bool isHoliday             // true if it's a holiday (AM has a holiday name)
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
    string hourCode,                        // E6, e.g. "1", "vak", "C123", etc.
    string singleDayTripCode,               // C123 ("Eendaagserit")
    double startTime,                       // F6
    double endTime,                         // G6
    double untaxedAllowanceNormalDayPartial,// AG6 result, i.e. same-day partial logic
    double dayRateBefore18,                 // AB6
    double eveningRateAfter18,              // AD6
    double lumpSumIf12h                     // AC6, TDonbelast for 12+ hours (cross-midnight)
)
{
    // 1) If the code is not "Eendaagserit", Excel returns "" => we do 0
    if (hourCode != singleDayTripCode)
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
        double shiftStart,      // Was F7 in Excel
        double shiftEnd,        // Was G7 in Excel
        double breakDuration,   // Was H7 in Excel (the break)
        double manualAdjustment // Was I7 in Excel
    )
    {
        double totalHours = (shiftEnd - shiftStart) - breakDuration + manualAdjustment;

        return Math.Round(totalHours, 2);
    }
}