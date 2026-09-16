namespace ProductionMeeting.ViewModels.Reports;

public class ReportRowViewModel
{
    public DateTime MeetingDate { get; set; }
    public string PlantName { get; set; } = null!;
    public string LineName { get; set; } = null!;
    public string IndicatorCode { get; set; } = null!;
    public string IndicatorName { get; set; } = null!;
    public string KpiDescription { get; set; } = null!;
    public string UnitName { get; set; } = null!;
    public string? ModelName { get; set; }
    public decimal? F26Value { get; set; }
    public decimal? F27Value { get; set; }
    public decimal? WeekValue { get; set; }
    public decimal? MonthCumValue { get; set; }
    public decimal? YtdValue { get; set; }
    public string? Remarks { get; set; }
}

public class ReportIndexViewModel
{
    public int? PlantId { get; set; }
    public int? LineId { get; set; }
    public int? IndicatorId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public List<(int Id, string Name)> Plants { get; set; } = new();
    public List<(int Id, string Name)> Lines { get; set; } = new();
    public List<(int Id, string Code, string Name)> Indicators { get; set; } = new();

    public List<ReportRowViewModel> Rows { get; set; } = new();

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
