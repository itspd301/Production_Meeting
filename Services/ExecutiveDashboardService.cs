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

        var currentSession = await _db.MeetingSessions
            .FirstOrDefaultAsync(s => s.PlantId == plantId && s.LineId == lineId && s.MeetingDate == filters.WeekStart, cancellationToken);

        var previousSession = await _db.MeetingSessions
            .Where(s => s.PlantId == plantId && s.LineId == lineId && s.MeetingDate < filters.WeekStart)
            .OrderByDescending(s => s.MeetingDate)
            .FirstOrDefaultAsync(cancellationToken);

        filters.SessionId = currentSession?.SessionId;
        filters.SessionStatus = currentSession?.Status == MeetingSessionStatus.Completed ? "Completed" : "Draft";
        filters.SessionRemarks = currentSession?.Remarks;
        filters.LastRefreshed = currentSession?.ModifiedDate ?? currentSession?.CreatedDate;

        var kpis = await _db.KpiMasters
            .Include(k => k.Indicator)
            .Include(k => k.Unit)
            .Where(k => k.IsActive && k.PlantId == plantId && k.LineId == lineId)
            .OrderBy(k => k.Indicator.IndicatorId).ThenBy(k => k.DisplayOrder)
            .ToListAsync(cancellationToken);

        var currentTx = currentSession == null
            ? new List<KpiTransaction>()
            : await _db.KpiTransactions.Where(t => t.SessionId == currentSession.SessionId).ToListAsync(cancellationToken);
        var previousTx = previousSession == null
            ? new List<KpiTransaction>()
            : await _db.KpiTransactions.Where(t => t.SessionId == previousSession.SessionId).ToListAsync(cancellationToken);

        filters.Groups = kpis
            .GroupBy(k => k.Indicator)
            .Select(g => new ExecutiveIndicatorGroupViewModel
            {
                IndicatorCode = g.Key.Code,
                IndicatorName = g.Key.Name,
                Kpis = g.Select(k =>
                {
                    var tx = currentTx.FirstOrDefault(t => t.KpiId == k.KpiId);
                    var prevTx = previousTx.FirstOrDefault(t => t.KpiId == k.KpiId);
                    var (status, variance) = KpiStatusCalculator.Compute(k.LowerIsBetter, tx?.WeekValue, tx?.F27Value);

                    var card = new ExecutiveKpiCardViewModel
                    {
                        KpiId = k.KpiId,
                        Description = k.Description,
                        UnitName = k.Unit.Name,
                        Value = tx?.WeekValue,
                        Target = tx?.F27Value,
                        PreviousValue = prevTx?.WeekValue,
                        MonthCumValue = tx?.MonthCumValue,
                        YtdValue = tx?.YtdValue,
                        Status = status,
                        Variance = variance
                    };
                    return card;
                }).ToList()
            }).ToList();

        filters.TotalCount = filters.Groups.Sum(g => g.Kpis.Count);
        filters.EnteredCount = filters.Groups.Sum(g => g.Kpis.Count(c => c.Value.HasValue));

        ApplyStaticPlaceholders(filters);

        return filters;
    }

    // Summary pills, charts and narrative are placeholders until their own stored
    // procedures are wired up (same pattern as DashboardService.TryGetScalarAsync for the
    // Straight Pass Ratio / Traceability cards). Replace each assignment below with a real
    // SP-backed value as those become available - the KPI cards and Weekly KPI Summary
    // table built above already use real data and don't need to change.
    private static void ApplyStaticPlaceholders(ExecutiveDashboardViewModel filters)
    {
        filters.SummaryFyLabel = "FY 25-26";
        filters.SummaryWeekLabel = "Week 32";
        filters.SummaryWeekDates = "04 Aug - 10 Aug 2025";
        filters.SummaryDataStatus = "Data Complete";
        filters.SummaryLastRefreshed = "04 Aug 2025, 06:45 PM";

        filters.TrendWeekLabels = ["W27", "W28", "W29", "W30", "W31", "W32"];
        filters.TrendSeries =
        [
            new ExecutiveTrendSeriesViewModel { Label = "Rejection Rate %", Values = [3.2m, 2.9m, 2.6m, 2.4m, 2.1m, 1.9m] },
            new ExecutiveTrendSeriesViewModel { Label = "On-Time Delivery %", Values = [92m, 93m, 94m, 95m, 96m, 97m] }
        ];
        filters.ManpowerSeries =
        [
            new ExecutiveTrendSeriesViewModel { Label = "Manpower Deployed", Values = [180m, 178m, 182m, 179m, 181m, 180m] }
        ];
        filters.TopDefects =
        [
            ("DDS", 150),
            ("K frame/cradle bolt half torque", 63),
            ("Paint chip", 42),
            ("Wiring harness clip", 28),
            ("Door fitment gap", 19)
        ];

        filters.Highlights =
        [
            new ExecutiveKpiCardViewModel { Description = "Safety - Near Miss Reporting", UnitName = "Nos", Value = 12m, Target = 10m },
            new ExecutiveKpiCardViewModel { Description = "Quality - First Time Right", UnitName = "%", Value = 97.4m, Target = 96m },
            new ExecutiveKpiCardViewModel { Description = "Delivery - On-Time Dispatch", UnitName = "%", Value = 98.1m, Target = 97m },
            new ExecutiveKpiCardViewModel { Description = "Cost - Scrap Cost", UnitName = "Rs. Lacs", Value = 2.1m, Target = 2.5m }
        ];
        filters.Lowlights =
        [
            new ExecutiveKpiCardViewModel { Description = "Quality - Rework RPT", UnitName = "Nos", Value = 245m, Target = 100m },
            new ExecutiveKpiCardViewModel { Description = "Production - Line Downtime", UnitName = "Hrs", Value = 6.5m, Target = 3m },
            new ExecutiveKpiCardViewModel { Description = "Cost - Overtime Cost", UnitName = "Rs. Lacs", Value = 4.2m, Target = 3m },
            new ExecutiveKpiCardViewModel { Description = "Morale - Absenteeism", UnitName = "%", Value = 5.8m, Target = 3m }
        ];
    }

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
