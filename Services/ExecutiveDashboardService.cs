using Microsoft.EntityFrameworkCore;
using ProductionMeeting.Data;
using ProductionMeeting.Helpers;
using ProductionMeeting.Models;
using ProductionMeeting.ViewModels;

namespace ProductionMeeting.Services;

public class ExecutiveDashboardService : IExecutiveDashboardService
{
    private const int TrendWeeks = 6;

    private readonly ApplicationDbContext _db;
    private readonly IUserAccessService _accessService;

    public ExecutiveDashboardService(ApplicationDbContext db, IUserAccessService accessService)
    {
        _db = db;
        _accessService = accessService;
    }

    public async Task<ExecutiveDashboardViewModel> GetDashboardAsync(ExecutiveDashboardViewModel filters, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var accessiblePlants = await _accessService.GetAccessiblePlantsAsync(userId, isAdmin, cancellationToken);
        filters.Plants = accessiblePlants.Select(p => (p.PlantId, p.Name)).ToList();

        filters.PlantId ??= accessiblePlants.FirstOrDefault()?.PlantId;

        if (filters.PlantId.HasValue)
        {
            var lines = await _accessService.GetAccessibleLinesAsync(userId, isAdmin, filters.PlantId.Value, cancellationToken);
            filters.Lines = lines.Select(l => (l.LineId, l.Name)).ToList();
            filters.LineId ??= filters.Lines.FirstOrDefault().Id;
            if (filters.LineId == 0) filters.LineId = null;
        }

        filters.WeekStart = PmDates.FromWeekInputValue(filters.Week) ?? PmDates.GetMondayOfWeek(DateTime.Today);
        filters.Week = PmDates.ToWeekInputValue(filters.WeekStart);

        filters.HasPlantAndShop = filters.PlantId.HasValue && filters.LineId.HasValue;
        if (!filters.HasPlantAndShop)
        {
            return filters;
        }

        var plantId = filters.PlantId!.Value;
        var lineId = filters.LineId!.Value;

        // The Remarks editor is a real, working feature (it saves to MeetingSessions), so
        // the session lookup underneath it stays live even though the KPI figures below are
        // all placeholders for now - see ApplyStaticPlaceholders.
        var currentSession = await _db.MeetingSessions
            .FirstOrDefaultAsync(s => s.PlantId == plantId && s.LineId == lineId && s.MeetingDate == filters.WeekStart, cancellationToken);

        filters.SessionId = currentSession?.SessionId;
        filters.SessionStatus = currentSession?.Status == MeetingSessionStatus.Completed ? "Completed" : "Draft";
        filters.SessionRemarks = currentSession?.Remarks;
        filters.LastRefreshed = currentSession?.ModifiedDate ?? currentSession?.CreatedDate;

        var kpis = await _db.KpiMasters
         .Include(k => k.Indicator)
         .Include(k => k.Unit)
         .Where(k => k.IsActive
                  && k.PlantId == plantId
                  && k.LineId == lineId)
         .OrderBy(k => k.Indicator.IndicatorId)
         .ThenBy(k => k.KpiId)
         .ToListAsync(cancellationToken);

        var transactions = currentSession == null
            ? new List<KpiTransaction>()
            : await _db.KpiTransactions
                .Where(t => t.SessionId == currentSession.SessionId)
                .ToListAsync(cancellationToken);

        filters.Groups = kpis
            .GroupBy(k => new
            {
                k.Indicator.Code,
                k.Indicator.Name
            })
            .Select(g => new ExecutiveIndicatorGroupViewModel
            {
                IndicatorCode = g.Key.Code,
                IndicatorName = g.Key.Name,
                Kpis = g.Select(k =>
                {
                    var tx = transactions.FirstOrDefault(t => t.KpiId == k.KpiId);

                    var (status, variance) =
                        KpiStatusCalculator.Compute(
                            k.LowerIsBetter,
                            tx?.WeekValue,
                            tx?.F27Value);

                    return new ExecutiveKpiCardViewModel
                    {
                        KpiId = k.KpiId,
                        Description = k.Description,
                        UnitName = k.Unit.Name,
                        Value = tx?.WeekValue,
                        Target = tx?.F27Value,
                        PreviousValue = null,
                        MonthCumValue = tx?.MonthCumValue,
                        YtdValue = tx?.YtdValue,
                        Status = status,
                        Variance = variance
                    };
                }).ToList()
            })
            .ToList();

        filters.TotalCount = filters.Groups.Sum(x => x.Kpis.Count);

        filters.EnteredCount = filters.Groups
            .Sum(x => x.Kpis.Count(k => k.Value.HasValue));

        filters.Highlights = filters.Groups
            .SelectMany(x => x.Kpis)
            .Where(x => x.Status == "Green")
            .Take(5)
            .ToList();

        filters.Lowlights = filters.Groups
            .SelectMany(x => x.Kpis)
            .Where(x => x.Status == "Red")
            .Take(5)
            .ToList();

        filters.SummaryWeekLabel =
            $"Week {System.Globalization.ISOWeek.GetWeekOfYear(filters.WeekStart)}";

        filters.SummaryWeekDates =
            $"{filters.WeekStart:dd MMM} - {filters.WeekStart.AddDays(6):dd MMM yyyy}";

        filters.SummaryDataStatus =
            filters.TotalCount == filters.EnteredCount
                ? "Data Complete"
                : "Data Pending";

        filters.SummaryFyLabel =
            $"FY {filters.WeekStart.Year % 100}-{(filters.WeekStart.Year + 1) % 100}";

        filters.SummaryLastRefreshed =
            filters.LastRefreshed?.ToString("dd MMM yyyy hh:mm tt") ?? "-";

        return filters;
    }

    // Everything below is placeholder/sample data until it's wired up to a stored
    // procedure (same pattern as DashboardService.TryGetScalarAsync for the Straight Pass
    // Ratio / Traceability cards). Replace each assignment with a real SP-backed value as
    // those become available - group by group, this can be swapped out incrementally.




    public async Task<ServiceResult> SaveRemarksAsync(int sessionId, string? remarks, string userId, CancellationToken cancellationToken = default)
    {
        var session = await _db.MeetingSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken);
        if (session == null)
        {
            return ServiceResult.Fail("Meeting session not found.");
        }

        session.Remarks = remarks;
        session.ModifiedBy = userId;
        session.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Remarks saved.");
    }

    public async Task<KpiTrendResult?> GetKpiTrendAsync(int kpiId, int plantId, int lineId, DateTime weekStart, CancellationToken cancellationToken = default)
    {
        var kpi = await _db.KpiMasters.Include(k => k.Unit).FirstOrDefaultAsync(k => k.KpiId == kpiId, cancellationToken);
        if (kpi == null)
        {
            return null;
        }

        var historySessions = await _db.MeetingSessions
            .Where(s => s.PlantId == plantId && s.LineId == lineId && s.MeetingDate <= weekStart)
            .OrderByDescending(s => s.MeetingDate)
            .Take(TrendWeeks)
            .ToListAsync(cancellationToken);
        historySessions.Reverse();

        var sessionIds = historySessions.Select(s => s.SessionId).ToList();
        var transactions = await _db.KpiTransactions
            .Where(t => t.KpiId == kpiId && sessionIds.Contains(t.SessionId))
            .ToListAsync(cancellationToken);

        var currentTx = transactions.FirstOrDefault(t => t.SessionId == historySessions.LastOrDefault(s => s.MeetingDate == weekStart)?.SessionId);

        return new KpiTrendResult
        {
            Description = kpi.Description,
            UnitName = kpi.Unit.Name,
            Target = currentTx?.F27Value,
            Points = historySessions.Select(s => new KpiTrendPoint
            {
                WeekLabel = $"W{ISOWeekNumber(s.MeetingDate)}",
                Value = transactions.FirstOrDefault(t => t.SessionId == s.SessionId)?.WeekValue
            }).ToList()
        };
    }

    private static int ISOWeekNumber(DateTime date) => System.Globalization.ISOWeek.GetWeekOfYear(date);
}
