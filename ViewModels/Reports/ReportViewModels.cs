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

public class WeeklyReportRowViewModel
{
    public int KpiId { get; set; }
    public string PlantName { get; set; } = "";
    public string LineName { get; set; } = "";
    public string IndicatorCode { get; set; } = "";
    public string KpiDescription { get; set; } = "";
    public string UnitName { get; set; } = "";
    public List<WeeklyReportValueViewModel> Values { get; set; } = new();
}

public class WeeklyReportValueViewModel
{
    public decimal? Value { get; set; }
    public decimal? Target { get; set; }
    public decimal? Change { get; set; }
    public string Status { get; set; } = "Pending";
    public string Comparison { get; set; } = "No previous value";
}

public class WeeklyReportViewModel
{
    public int? PlantId { get; set; }
    public int? LineId { get; set; }
    public int? IndicatorId { get; set; }
    public string StartWeek { get; set; } = "";
    public string EndWeek { get; set; } = "";
    public DateTime StartWeekStart { get; set; }
    public DateTime EndWeekStart { get; set; }
    public string? RangeError { get; set; }

    public List<(int Id, string Name)> Plants { get; set; } = new();
    public List<(int Id, string Name)> Lines { get; set; } = new();
    public List<(int Id, string Code, string Name)> Indicators { get; set; } = new();
    public List<(DateTime Start, string Label)> Weeks { get; set; } = new();
    public List<WeeklyReportRowViewModel> Rows { get; set; } = new();

    public int TotalCount { get; set; }
    public int EnteredCount { get; set; }
    public int GreenCount { get; set; }
    public int AmberCount { get; set; }
    public int RedCount { get; set; }
    public int ImprovedCount { get; set; }
    public int DeclinedCount { get; set; }
}
