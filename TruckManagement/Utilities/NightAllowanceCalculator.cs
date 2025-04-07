using TruckManagement.Entities;

namespace TruckManagement.Utilities;

public class NightAllowanceCalculator
{
    
    private readonly Cao _cao;

    public NightAllowanceCalculator(Cao cao)
    {
        _cao = cao ?? throw new ArgumentNullException(nameof(cao));
    }
    
    /// <summary>
    /// Calculates the night allowance for a single shift, mimicking the logic 
    /// in "Periode_1" sheet, cell O6, using named parameters for clarity.
    /// </summary>
    /// <param name="startTime">Shift start time in decimal hours (was F6).</param>
    /// <param name="endTime">Shift end time in decimal hours (was G6).</param>
    /// <param name="nightHoursAllowed">Admin E28 = "ja"? True if night hours are enabled.</param>
    /// <param name="driverRate">Admin C15: e.g. 19.46, the driver’s first hourly rate.</param>
    /// <param name="nightHoursWholeHours">Admin K27 = "ja"? If true, maybe round hours to whole hours.</param>
    /// <returns>The total night allowance in decimal currency.</returns>
    public double CalculateNightAllowance(
        double startTime,
        double endTime,
        bool nightHoursAllowed,
        double driverRate,
        bool nightHoursWholeHours
    )
    {
        // 1) If night hours not allowed => return 0
        if (!nightHoursAllowed)
            return 0.0;
        
        double nightStart = _cao.NightTimeStart.TotalHours; // e.g. 21.0
        double nightEnd   = _cao.NightTimeEnd.TotalHours;   // e.g. 5.0
        // 3) Calculate how many night hours are in the shift
        //    We'll define "night hours" as the portion overlapping [nightStartTime, 24) plus (0, nightEndTime].
        //    For example, if nightStartTime=21 and nightEndTime=5, 
        //    then the night window is 21:00-24:00 plus 00:00-05:00.

        double nightHours = CalculateNightHours(startTime, endTime, nightStart, nightEnd);

        // 4) Possibly round or floor to whole hours if nightHoursWholeHours is true
        if (nightHoursWholeHours)
        {
            nightHours = Math.Floor(nightHours);
        }

        // 5) Convert those night hours into an allowance
        double nightAllowance = nightHours * (driverRate * (double)_cao.NightHoursAllowanceRate);

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