namespace TruckManagement
{
    public class UpdatePartRideRequest
    {
        public string? CompanyId { get; set; }
        public string? RideId { get; set; }
        public DateTime? Date { get; set; }
        public TimeSpan? Start { get; set; }
        public TimeSpan? End { get; set; }
        public double? Kilometers { get; set; }
        public string? CarId { get; set; }
        public string? DriverId { get; set; }
        public decimal? Costs { get; set; }
        public string? ClientId { get; set; }
        public int? WeekNumber { get; set; }
        public string? UnitId { get; set; }
        public string? RateId { get; set; }
        public string? CostsDescription { get; set; }
        public string? SurchargeId { get; set; }
        public decimal? Turnover { get; set; }
        public string? Remark { get; set; }
        public string? CharterId { get; set; }
        public string? HoursCodeId { get; set; }
        public string? HoursOptionId { get; set; }
        public double? HoursCorrection { get; set; }
        public double? VariousCompensation { get; set; }
    }
}