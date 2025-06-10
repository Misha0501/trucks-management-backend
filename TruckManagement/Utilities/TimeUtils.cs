namespace TruckManagement.Utilities
{
    public static class TimeUtils
    {
        public static TimeSpan ParseTimeString(string timeStr)
        {
            if (string.IsNullOrWhiteSpace(timeStr))
            {
                throw new FormatException("Time string cannot be empty or null.");
            }

            if (timeStr.Trim().ToLower() == "24:00:00")
            {
                return TimeSpan.FromHours(24); // Convert "24:00:00" to 1.00:00:00 (24 hours)
            }

            return TimeSpan.Parse(timeStr); // Parse other times like "23:59:59" normally
        }
    }
}