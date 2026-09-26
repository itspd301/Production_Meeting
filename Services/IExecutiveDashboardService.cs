using ProductionMeeting.ViewModels;

namespace ProductionMeeting.Services;

public class KpiTrendPoint
{
    public string WeekLabel { get; set; } = null!;
    public decimal? Value { get; set; }
}

public class KpiTrendResult
{
    public string Description { get; set; } = null!;
    public string UnitName { get; set; } = null!;
    public decimal? Target { get; set; }
    public List<KpiTrendPoint> Points { get; set; } = new();
}

public interface IExecutiveDashboardService
{
    Task<ExecutiveDashboardViewModel> GetDashboardAsync(ExecutiveDashboardViewModel filters, string userId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<ServiceResult> SaveRemarksAsync(int sessionId, string? remarks, string userId, CancellationToken cancellationToken = default);

    Task<KpiTrendResult?> GetKpiTrendAsync(int kpiId, int plantId, int lineId, DateTime weekStart, CancellationToken cancellationToken = default);
}
