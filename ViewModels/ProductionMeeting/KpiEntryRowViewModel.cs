namespace ProductionMeeting.ViewModels.ProductionMeeting;

public class KpiEntryRowViewModel
{
    public int KpiId { get; set; }
    public string Description { get; set; } = null!;
    public string UnitName { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public string? Source { get; set; }
    public bool IsReadOnlySource => Source == Helpers.KpiSources.StoredProcedure;

    public int? TransactionId { get; set; }
    public int? ModelId { get; set; }

    public decimal? F26Value { get; set; }
    public decimal? F27Value { get; set; }
    public decimal? WeekValue { get; set; }
    public decimal? MonthCumValue { get; set; }
    public decimal? YtdValue { get; set; }
    public string? Remarks { get; set; }
}

public class IndicatorGroupViewModel
{
    public int IndicatorId { get; set; }
    public string IndicatorCode { get; set; } = null!;
    public string IndicatorName { get; set; } = null!;
    public List<KpiEntryRowViewModel> Rows { get; set; } = new();
}
