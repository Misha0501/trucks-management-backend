namespace TruckManagement.Utilities;

public static class NightAllowanceCalculator
{
    /// <summary>
    /// Calculates the night allowance for a single shift, mimicking the logic 
    /// in "Periode_1" sheet, cell O6, using named parameters for clarity.
    /// </summary>
    /// <param name="inputDate">The date of the shift (was D6 in Excel).</param>
    /// <param name="startTime">Shift start time in decimal hours (was F6).</param>
    /// <param name="endTime">Shift end time in decimal hours (was G6).</param>
    /// <param name="nightStartTime">When night window begins (AJ1), e.g. 21.0.</param>
    /// <param name="nightEndTime">When night window ends (AK1), e.g. 5.0.</param>
    /// <param name="nightHoursAllowed">Admin E28 = "ja"? True if night hours are enabled.</param>
    /// <param name="nightHours19Percent">Admin K28 = "ja"? True if night hours are 19% surcharge.</param>
    /// <param name="nightHoursInEuros">Admin H28 = "ja"? True if we pay night hours in euros rate.</param>
    /// <param name="someMonthDate">Admin D15: a reference month date for rate selection.</param>
    /// <param name="driverRateOne">Admin C15: e.g. 19.46, the driver’s first hourly rate.</param>
    /// <param name="driverRateTwo">Admin E15: e.g. 18.71, the driver’s second hourly rate.</param>
    /// <param name="nightAllowanceRate">Admin D28: e.g. 0.19, if we’re adding 19% or 0.19 euros/hour.</param>
    /// <param name="nightHoursWholeHours">Admin K27 = "ja"? If true, maybe round hours to whole hours.</param>
    /// <returns>The total night allowance in decimal currency.</returns>
    public static double CalculateNightAllowance(
        DateTime inputDate,
        double startTime,
        double endTime,
        double nightStartTime,
        double nightEndTime,
        bool nightHoursAllowed,
        bool nightHours19Percent,
        bool nightHoursInEuros,
        DateTime someMonthDate,
        double driverRateOne,
        double driverRateTwo,
        double nightAllowanceRate,
        bool nightHoursWholeHours
    )
    {
        // 1) If night hours not allowed => return 0
        if (!nightHoursAllowed)
            return 0.0;

        // 2) Determine which driver rate to use based on the month comparison
        //    If the input date's month is strictly less than someMonthDate's month, use driverRateOne
        //    else use driverRateTwo
        double chosenDriverRate;
        if (inputDate.Month < someMonthDate.Month || inputDate.Year < someMonthDate.Year)
        {
            chosenDriverRate = driverRateOne;
        }
        else
        {
            chosenDriverRate = driverRateTwo;
        }

        // 3) Calculate how many night hours are in the shift
        //    We'll define "night hours" as the portion overlapping [nightStartTime, 24) plus (0, nightEndTime].
        //    For example, if nightStartTime=21 and nightEndTime=5, 
        //    then the night window is 21:00-24:00 plus 00:00-05:00.

        double nightHours = CalculateNightHours(startTime, endTime, nightStartTime, nightEndTime);

        // 4) Possibly round or floor to whole hours if nightHoursWholeHours is true
        if (nightHoursWholeHours)
        {
            nightHours = Math.Floor(nightHours);
        }

        // 5) Convert those night hours into an allowance
        double nightAllowance = 0.0;

        // 5a) If the spreadsheet says "nightHours19Percent" => 
        //     the original might multiply by AdminD10 or something. 
        //     We'll interpret that as "19% of the normal pay" or "some factor of base pay."
        //     If "nightHoursInEuros", we interpret it as "night hours * (driverRate * nightAllowanceRate)."

        if (nightHours19Percent)
        {
            // For example, 19% might mean: totalNightPay = nightHours * (chosenDriverRate * 0.19)
            // If your sheet used a direct multiplier from an AdminD10, you can put that in place of nightAllowanceRate.
            nightAllowance = nightHours * (chosenDriverRate * nightAllowanceRate);
        }
        else if (nightHoursInEuros)
        {
            // If it's a direct euros approach, e.g. "night hours in euros"
            // Possibly: nightHours * (some base euro rate).
            // We'll assume "chosenDriverRate * nightAllowanceRate" again
            nightAllowance = nightHours * (chosenDriverRate * nightAllowanceRate);
        }
        else
        {
            // If none of the flags is set, no extra pay
            nightAllowance = 0.0;
        }

        return Math.Round(nightAllowance, 2);
    }

    /// <summary>
    /// Calculates how many hours of the shift (startTime -> endTime) fall into the "night window" 
    /// from nightStartTime -> 24, plus 0 -> nightEndTime.
    /// 
    /// If endTime < startTime, we assume crossing midnight. 
    /// Example: nightStart=21, nightEnd=5 => night window is 21:00-24:00 plus 00:00-05:00.
    /// </summary>
    private static double CalculateNightHours(
        double startTime, 
        double endTime, 
        double nightStart, 
        double nightEnd
    )
    {
        // Convert everything to a 0-24 range. We have a typical approach:
        // If endTime < startTime => crosses midnight. 
        // We'll unify that in a single function that accumulates hours in [nightStart..24) + [0..nightEnd].
        
        // Shift might cross midnight or not. We can break it into segments:
        double totalNight = 0.0;

        // Let's define a small local function to clamp times to a range [0..24].
        // For example, if start=22 and end=26 => end=2 after midnight, we can handle it with a loop
        // Or we can do a simpler approach: break the shift into two segments if crossing midnight.

        if (endTime < startTime)
        {
            // CROSS MIDNIGHT: (startTime..24) plus (0..endTime)
            // We'll sum the night hours in each part.
            totalNight += OverlapNight(startTime, 24, nightStart, nightEnd);
            totalNight += OverlapNight(0, endTime, nightStart, nightEnd);
        }
        else
        {
            // No crossing midnight
            totalNight += OverlapNight(startTime, endTime, nightStart, nightEnd);
        }

        return totalNight;
    }

    /// <summary>
    /// Computes overlap of [shiftStart..shiftEnd] with the "night window" [nightStart..24) plus [0..nightEnd].
    /// But to keep it simpler, let's create an adjusted method to handle just a single continuous band.
    /// 
    /// Actually, in many "night shift" definitions, the window is effectively 21..24 plus 0..5 
    /// so we can do that in two calls if we want.
    /// 
    /// For generality, let's just do one call for [nightStart..24], another for [0..nightEnd], 
    /// outside if nightStart < nightEnd. 
    /// Here we handle a single overlap of [shiftStart..shiftEnd] with [nightStart..nightEnd], 
    /// assuming nightStart <= nightEnd. 
    /// If nightStart > nightEnd (like 21->5), we break it into two calls: [21..24] + [0..5].
    /// </summary>
    private static double OverlapNight(
        double shiftStart, 
        double shiftEnd, 
        double nightStart, 
        double nightEnd
    )
    {
        // If nightStart < nightEnd (like 0..5), do direct overlap
        // If nightStart > nightEnd (like 21..5), we need to break it up into (21..24) + (0..5).
        // For simplicity: we detect that and do two partial overlaps.

        if (nightStart < nightEnd)
        {
            // straightforward overlap
            return Overlap(shiftStart, shiftEnd, nightStart, nightEnd);
        }
        else
        {
            // night window crosses midnight, e.g. 21..24 + 0..5 
            // overlap with first part:
            double part1 = Overlap(shiftStart, shiftEnd, nightStart, 24);
            // overlap with second part:
            double part2 = Overlap(shiftStart, shiftEnd, 0, nightEnd);
            return part1 + part2;
        }
    }

    /// <summary>
    /// Returns length of overlap (in hours) between two intervals: [startA..endA] and [startB..endB].
    /// All in 0..24 scale.
    /// </summary>
    private static double Overlap(double startA, double endA, double startB, double endB)
    {
        double start = Math.Max(startA, startB);
        double end = Math.Min(endA, endB);
        double overlap = end - start;
        return (overlap > 0) ? overlap : 0.0;
    }
}