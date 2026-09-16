namespace ProductionMeeting.ViewModels.ProductionMeeting;

public class MeetingListItemViewModel
{
    public int SessionId { get; set; }
    public string PlantName { get; set; } = null!;
    public string LineName { get; set; } = null!;
    public string? ShiftName { get; set; }
    public DateTime MeetingDate { get; set; }
    public string Status { get; set; } = null!;
    public string ConductedByName { get; set; } = null!;
    public int KpiCount { get; set; }
    public int EnteredCount { get; set; }
}
