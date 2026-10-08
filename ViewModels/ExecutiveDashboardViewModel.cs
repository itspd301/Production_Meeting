namespace ProductionMeeting.ViewModels;

public class ExecutiveKpiCardViewModel
{
    public int KpiId { get; set; }
    public string Description { get; set; } = null!;
    public string UnitName { get; set; } = null!;
    public decimal? Value { get; set; }
    public decimal? Target { get; set; }
    public decimal? PreviousValue { get; set; }
    public decimal? MonthCumValue { get; set; }
    public decimal? YtdValue { get; set; }
    public string Status { get; set; } = "Pending";
    public decimal? Variance { get; set; }
    public decimal? WeekOverWeek => Value.HasValue && PreviousValue.HasValue ? Value - PreviousValue : null;
}

public class ExecutiveIndicatorGroupViewModel
{
    public string IndicatorCode { get; set; } = null!;
    public string IndicatorName { get; set; } = null!;
    public List<ExecutiveKpiCardViewModel> Kpis { get; set; } = new();
}

public class ExecutiveTrendSeriesViewModel
{
    public string Label { get; set; } = null!;
    public List<decimal?> Values { get; set; } = new();
}

public class ManpowerShiftViewModel
{
    public string ShiftName { get; set; } = "";
    public int AssociateCount { get; set; }
    public int RequiredWorkstations { get; set; }
    public int Deployment { get; set; }
}

public class ManpowerDayViewModel
{
    public DateTime Date { get; set; }
    public int AssociateCount { get; set; }
    public int RequiredWorkstations { get; set; }
    public int Deployment { get; set; }
    public List<ManpowerShiftViewModel> Shifts { get; set; } = new();
}

public class ExecutiveDashboardViewModel
{
    // Filters
    public int? PlantId { get; set; }
    public int? LineId { get; set; }
    public string Week { get; set; } = null!;

    public List<(int Id, string Name)> Plants { get; set; } = new();
    public List<(int Id, string Name)> Lines { get; set; } = new();

    public bool HasPlantAndShop { get; set; }
    public DateTime WeekStart { get; set; }
    public int? SessionId { get; set; }
    public string SessionStatus { get; set; } = "Draft";
    public string? SessionRemarks { get; set; }
    public DateTime? LastRefreshed { get; set; }
    public bool CanEditRemarks { get; set; }

    public List<ExecutiveIndicatorGroupViewModel> Groups { get; set; } = new();

    // Summary pills, charts and narrative below are placeholder/sample data pending
    // stored-procedure integration (see ExecutiveDashboardService.ApplyStaticPlaceholders).
    // The KPI cards above and the Weekly KPI Summary table are real, live data.
    public string SummaryFyLabel { get; set; } = "";
    public string SummaryWeekLabel { get; set; } = "";
    public string SummaryWeekDates { get; set; } = "";
    public string SummaryDataStatus { get; set; } = "";
    public string SummaryLastRefreshed { get; set; } = "";

    public List<string> TrendWeekLabels { get; set; } = new();
    public List<ExecutiveTrendSeriesViewModel> TrendSeries { get; set; } = new();
    public List<ExecutiveTrendSeriesViewModel> ManpowerSeries { get; set; } = new();
    public List<ManpowerDayViewModel> ManpowerDays { get; set; } = new();
    public List<(string Name, int Count)> TopDefects { get; set; } = new();

    public List<ExecutiveKpiCardViewModel> Highlights { get; set; } = new();
    public List<ExecutiveKpiCardViewModel> Lowlights { get; set; } = new();

    public int TotalCount { get; set; }
    public int EnteredCount { get; set; }
}
