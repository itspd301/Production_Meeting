using ProductionMeeting.Models;
using ProductionMeeting.ViewModels.ProductionMeeting;

namespace ProductionMeeting.Services;

public class SaveEntryResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;

    // Server-calculated Month Cum./YTD per KPI, so the grid can show the new running
    // totals without a full page reload.
    public List<SavedRowTotals> Rows { get; set; } = new();
}

public class SavedRowTotals
{
    public int KpiId { get; set; }
    public decimal? MonthCumValue { get; set; }
    public decimal? YtdValue { get; set; }
}

public interface IProductionMeetingService
{
    Task<MeetingIndexViewModel> GetIndexAsync(MeetingIndexViewModel filters, string userId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<MeetingEntryViewModel?> GetOrCreateEntryAsync(int plantId, int lineId, int? shiftId, DateTime meetingDate, string userId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<MeetingEntryViewModel?> GetEntryBySessionIdAsync(int sessionId, string userId, bool isAdmin, bool readOnly, CancellationToken cancellationToken = default);

    Task<SaveEntryResult> SaveEntryAsync(SaveEntryRequest request, string userId, string userFullName, CancellationToken cancellationToken = default);

    Task<List<KpiTransactionAudit>> GetSessionAuditHistoryAsync(int sessionId, CancellationToken cancellationToken = default);
}
