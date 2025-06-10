namespace TruckManagement.Utilities
{
    public static class TimeUtils
    {
        public static TimeSpan ParseTimeString(string? timeStr)
        {
            if (string.IsNullOrWhiteSpace(timeStr))
            {
                throw new FormatException("Time string cannot be empty or null.");
            }

            TimeSpan result;
            string trimmedTimeStr = timeStr.Trim().ToLower();

            if (trimmedTimeStr == "24:00:00")
            {
                result = TimeSpan.FromHours(24); // Convert "24:00:00" to 1.00:00:00 (24 hours)
            }
            else if (trimmedTimeStr.Contains(":"))
            {
                result = TimeSpan.Parse(timeStr); // Parse times in "HH:mm:ss" format
            }
            else
            {
                // Try parsing as decimal hours (e.g., "10.5" for 10 hours 30 minutes)
                if (!double.TryParse(timeStr, out double decimalHours))
                {
                    throw new FormatException("Invalid time format. Use 'HH:mm:ss' or decimal hours (e.g., '10.5').");
                }

                // Convert decimal hours to TimeSpan
                result = TimeSpan.FromHours(decimalHours);
            }

            // Enforce minimum (00:00:00) and maximum (24:00:00 or 1.00:00:00)
            if (result.TotalHours < 0)
            {
                throw new FormatException("Time cannot be less than 00:00:00.");
            }
            if (result.TotalHours > 24)
            {
                throw new FormatException("Time cannot be greater than 24:00:00.");
            }

            return result;
        }
    }
}