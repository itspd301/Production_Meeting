using ProductionMeeting.Helpers;

namespace ProductionMeeting.ViewModels;

public class DashboardCardViewModel
{
    public string Label { get; set; } = null!;
    public string ValueDisplay { get; set; } = null!;
    public string Status { get; set; } = "neutral"; // good | bad | warn | neutral
    public string? SubText { get; set; }
}

public class DashboardKpiRowViewModel
{
    public int KpiId { get; set; }
    public string IndicatorCode { get; set; } = null!;
    public string IndicatorName { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string UnitName { get; set; } = null!;
    public string Source { get; set; } = KpiSources.Manual;
    public decimal? Target { get; set; }
    public decimal? WeekValue { get; set; }
    public decimal? Variance { get; set; }
    public decimal? MonthCumValue { get; set; }
    public decimal? YtdValue { get; set; }
    public string Status { get; set; } = "Pending"; // Green | Amber | Red | Pending | NoTarget
    public string? Remarks { get; set; }
}

public class DashboardViewModel
{
    // Filters
    public int? PlantId { get; set; }
    public int? LineId { get; set; }
    public string Week { get; set; } = PmDates.ToWeekInputValue(DateTime.Today);

    public List<(int Id, string Name)> Plants { get; set; } = new();
    public List<(int Id, string Name)> Lines { get; set; } = new();

    public bool HasPlantAndShop { get; set; }
    public DateTime WeekStart { get; set; }
    public int? SessionId { get; set; }
    public string SessionStatus { get; set; } = "Draft";

    public List<DashboardCardViewModel> Cards { get; set; } = new();
    public List<(string Code, string Name)> Categories { get; set; } = new();
    public List<DashboardKpiRowViewModel> Rows { get; set; } = new();
    public List<DashboardKpiRowViewModel> AttentionRequired { get; set; } = new();

    public int EnteredCount { get; set; }
    public int TotalCount { get; set; }
}
