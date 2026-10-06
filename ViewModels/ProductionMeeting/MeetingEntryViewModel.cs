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

    // Fiscal year (April-March) based column labels for the F26/F27 columns - shift
    // forward every April (see PmDates.FiscalTargetLabels).
    public string F26Label { get; set; } = null!;
    public string F27Label { get; set; } = null!;

    public List<IndicatorGroupViewModel> IndicatorGroups { get; set; } = new();
}
