using ProductionMeeting.ViewModels;

namespace ProductionMeeting.Services;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardAsync(DashboardViewModel filters, string userId, bool isAdmin, CancellationToken cancellationToken = default);
}
