using ProductionMeeting.ViewModels;

namespace ProductionMeeting.Services;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default);
}
