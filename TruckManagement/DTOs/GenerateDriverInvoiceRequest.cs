namespace TruckManagement.DTOs
{
    /// <summary>
    /// Request to generate a driver invoice for a specific week.
    /// Values can be modified by the driver before generation.
    /// </summary>
    public class GenerateDriverInvoiceRequest
    {
        public int Year { get; set; }
        public int WeekNumber { get; set; }
        
        /// <summary>
        /// Total hours worked (can be modified by driver)
        /// </summary>
        public decimal HoursWorked { get; set; }
        
        /// <summary>
        /// Hourly compensation (can be modified by driver)
        /// </summary>
        public decimal HourlyCompensation { get; set; }
        
        /// <summary>
        /// Additional compensation (can be modified by driver)
        /// </summary>
        public decimal AdditionalCompensation { get; set; }
    }
}

