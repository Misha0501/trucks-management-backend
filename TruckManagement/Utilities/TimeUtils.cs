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
            else
            {
                result = TimeSpan.Parse(timeStr); // Parse other times like "23:59:59" normally
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