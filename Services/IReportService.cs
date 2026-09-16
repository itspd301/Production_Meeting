using ProductionMeeting.ViewModels.Reports;

namespace ProductionMeeting.Services;

public interface IReportService
{
    Task<ReportIndexViewModel> GetReportAsync(ReportIndexViewModel filters, string userId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<List<ReportRowViewModel>> GetReportRowsForExportAsync(ReportIndexViewModel filters, string userId, bool isAdmin, CancellationToken cancellationToken = default);
}
