namespace TruckManagement
{
    public class UpdatePartRideRequest
    {
        public string? CompanyId { get; set; }
        public string? RideId { get; set; }
        public DateTime? Date { get; set; }
        public string? Start { get; set; }
        public string? End { get; set; } 
        public double? Kilometers { get; set; }
        public string? CarId { get; set; }
        public string? DriverId { get; set; }
        public decimal? Costs { get; set; }
        public string? ClientId { get; set; }
        public int? WeekNumber { get; set; }
        public string? CostsDescription { get; set; }
        public decimal? Turnover { get; set; }
        public string? Remark { get; set; }
        public string? CharterId { get; set; }
        public string? HoursCodeId { get; set; }
        public string? HoursOptionId { get; set; }
        public double? HoursCorrection { get; set; }
        public double? VariousCompensation { get; set; }
        public List<Guid>? UploadIds { get; set; }
        public List<Guid>? NewUploadIds { get; set; }
    }
}