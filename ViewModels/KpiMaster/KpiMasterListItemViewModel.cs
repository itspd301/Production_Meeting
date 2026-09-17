namespace ProductionMeeting.ViewModels.KpiMaster;

public class KpiMasterListItemViewModel
{
    public int KpiId { get; set; }
    public string PlantName { get; set; } = null!;
    public string LineName { get; set; } = null!;
    public string IndicatorCode { get; set; } = null!;
    public string IndicatorName { get; set; } = null!;
    public string UnitName { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public class KpiMasterIndexViewModel
{
    public string? Search { get; set; }
    public int? PlantId { get; set; }
    public int? LineId { get; set; }

    public List<(int Id, string Name)> Plants { get; set; } = new();
    public List<(int Id, string Name)> Lines { get; set; } = new();

    public List<KpiMasterListItemViewModel> Items { get; set; } = new();

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
