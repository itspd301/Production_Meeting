namespace ProductionMeeting.ViewModels.ProductionMeeting;

public class KpiEntryRowViewModel
{
    public int KpiId { get; set; }
    public string Description { get; set; } = null!;
    public string UnitName { get; set; } = null!;
    public int DisplayOrder { get; set; }

    public int? TransactionId { get; set; }

    // Pre-filled from KpiMaster's fixed yearly value when no week-specific override has
    // been saved yet (see ProductionMeetingService.BuildEntryViewModelAsync). The Entry
    // grid still lets someone override just this week's value, with a confirm popup.
    public decimal? F26Value { get; set; }
    public decimal? F27Value { get; set; }
    public decimal? MasterF26Value { get; set; }
    public decimal? MasterF27Value { get; set; }

    public decimal? WeekValue { get; set; }

    // Server-calculated running totals (see ProductionMeetingService.ComputeCumulativeValues) -
    // not directly editable on the grid.
    public decimal? MonthCumValue { get; set; }
    public decimal? YtdValue { get; set; }
    public string? Remarks { get; set; }

    // True when this KPI is configured in KPI Master to auto-fill its Week value from a
    // stored procedure. The fetched value is still just a starting point - the user can
    // type over it like any other cell.
    public bool IsSpSourced { get; set; }
}

public class IndicatorGroupViewModel
{
    public int IndicatorId { get; set; }
    public string IndicatorCode { get; set; } = null!;
    public string IndicatorName { get; set; } = null!;
    public List<KpiEntryRowViewModel> Rows { get; set; } = new();
}
