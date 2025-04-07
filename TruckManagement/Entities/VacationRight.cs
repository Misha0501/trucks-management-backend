namespace TruckManagement.Entities;

public class VacationRight
{
    public int Id { get; set; }
    public int? AgeFrom { get; set; }
    public int? AgeTo { get; set; }
    public string Description { get; set; } = default!;
    public int Right { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}