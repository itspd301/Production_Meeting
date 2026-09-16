namespace ProductionMeeting.ViewModels.ProductionMeeting;

public class SaveEntryRequest
{
    public int SessionId { get; set; }
    public List<SaveEntryRow> Rows { get; set; } = new();
    public bool MarkCompleted { get; set; }
}

public class SaveEntryRow
{
    public int KpiId { get; set; }
    public int? ModelId { get; set; }
    public decimal? F26Value { get; set; }
    public decimal? F27Value { get; set; }
    public decimal? WeekValue { get; set; }
    public decimal? MonthCumValue { get; set; }
    public decimal? YtdValue { get; set; }
    public string? Remarks { get; set; }
}
