namespace ProductionMeeting.ViewModels.ProductionMeeting;

public class MeetingEntryViewModel
{
    public int SessionId { get; set; }
    public int PlantId { get; set; }
    public string PlantName { get; set; } = null!;
    public int LineId { get; set; }
    public string LineName { get; set; } = null!;
    public int? ShiftId { get; set; }
    public DateTime MeetingDate { get; set; }
    public string Status { get; set; } = null!;
    public string? Remarks { get; set; }
    public bool ReadOnly { get; set; }

    public List<(int Id, string Name)> Models { get; set; } = new();
    public List<IndicatorGroupViewModel> IndicatorGroups { get; set; } = new();
}
