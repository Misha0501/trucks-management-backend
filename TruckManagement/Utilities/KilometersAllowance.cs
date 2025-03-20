namespace TruckManagement;

public class KilometersAllowance
{
    public static double HomeWorkDistance(
        bool kilometerAllowanceEnabled,  // Admin!E16 == "ja"?
        double oneWayValue,             // Admin!G16
        double minThreshold,            // Admin!H16
        double maxThreshold             // Admin!I16
    )
    {
        // 1) If the allowance is not enabled => 0
        if (!kilometerAllowanceEnabled)
        {
            return 0.0;
        }

        // 2) If oneWayValue < min => 0
        if (oneWayValue < minThreshold)
        {
            return 0.0;
        }

        // 3) If oneWayValue > max => max - min
        if (oneWayValue > maxThreshold)
        {
            return maxThreshold - minThreshold;
        }

        // 4) Otherwise => oneWayValue - min
        return oneWayValue - minThreshold;
    }
    
     /// <summary>
    /// Replicates the cell P6 formula from the "Periode_1" sheet.
    /// </summary>
    /// <param name="extraKilometers">Q6, the extra km traveled.</param>
    /// <param name="kilometerRate">P3, the allowance rate per km.</param>
    /// <param name="hourCode">E6, a numeric or string code: e.g. "3", "vak", etc.</param>
    /// <param name="hourOption">J6, e.g. "X", "GW", or empty.</param>
    /// <param name="totalHours">K6, total shift hours.</param>
    /// <param name="homeWorkDistance">P2, the home–work distance factor.</param>
    /// <returns>The value that goes into P6.</returns>
    public static double CalculateKilometersAllowance(
        double extraKilometers,
        double kilometerRate,
        string hourCode,
        string hourOption,
        double totalHours,
        double homeWorkDistance
    )
    {
        // 1) Base part: Q6 * P3
        double result = extraKilometers * kilometerRate;

        // 2) Now the big IF from Excel:
        //    If hourCode is in {3, "vak", "zie", "tvt", 0} or hourOption in {"X", "GW"}, add nothing.
        if (ShouldSkip(hourCode, hourOption))
        {
            // We add 0, do nothing.
            return result;
        }

        // 3) If hourCode is in {5,7,8,9,10,12,13,15,16,17} => add 0 => do nothing
        if (IsInSet(hourCode, new[] { "5","7","8","9","10","12","13","15","16","17" }))
        {
            return result;
        }

        // 4) If hourCode is a numeric >18 and <25 => add 0 => do nothing
        //    We'll parse the hourCode as double if possible
        if (IsBetween18And25(hourCode))
        {
            return result;
        }

        // 5) If totalHours > 0, we add either (P2 * P3) or (2 * P2 * P3) depending on E6 in {2,4}
        if (totalHours > 0)
        {
            double secondTerm = 0.0;
            if (IsInSet(hourCode, new[] { "2", "4" }))
            {
                // E6=2 or 4 => P2 * P3
                secondTerm = homeWorkDistance * kilometerRate;
            }
            else
            {
                // otherwise => 2*P2 * P3
                secondTerm = 2.0 * homeWorkDistance * kilometerRate;
            }

            result += secondTerm;
        }

        return result;
    }

    // ---------- Helper Methods ----------

    // Replicates: OR(E6=3; E6="vak"; E6="zie"; E6="tvt"; E6=0; J6="X"; J6="GW")
    private static bool ShouldSkip(string code, string option)
    {
        var skipCodes = new HashSet<string> { "3", "vak", "zie", "tvt", "0" };
        var skipOptions = new HashSet<string> { "X", "GW" };

        if (skipCodes.Contains(code))
            return true;

        if (skipOptions.Contains(option))
            return true;

        return false;
    }

    // Checks if code is in the given string set
    private static bool IsInSet(string code, string[] set)
    {
        foreach (var s in set)
        {
            if (code == s) return true;
        }
        return false;
    }

    // Checks if code can be parsed as a number and is between 18 and 25 (exclusive)
    private static bool IsBetween18And25(string code)
    {
        if (double.TryParse(code, out double num))
        {
            // AND(E6>18; E6<25)
            if (num > 18.0 && num < 25.0)
                return true;
        }
        return false;
    }
}