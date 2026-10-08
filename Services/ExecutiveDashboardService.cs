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
    private readonly ILogger<ExecutiveDashboardService> _logger;

    public ExecutiveDashboardService(
        ApplicationDbContext db,
        IUserAccessService accessService,
        ILogger<ExecutiveDashboardService> logger)
    {
        _db = db;
        _accessService = accessService;
        _logger = logger;
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
            $"Week {PmDates.FiscalWeekNumber(filters.WeekStart)}";

        filters.SummaryWeekDates =
            $"{filters.WeekStart:dd MMM} - {filters.WeekStart.AddDays(6):dd MMM yyyy}";

        filters.SummaryDataStatus =
            filters.TotalCount == filters.EnteredCount
                ? "Data Complete"
                : "Data Pending";

        var fyEnd = PmDates.FiscalYearEndYearOfWeek(filters.WeekStart);
        filters.SummaryFyLabel = $"FY {(fyEnd - 1) % 100:D2}-{fyEnd % 100:D2}";

        filters.SummaryLastRefreshed =
            filters.LastRefreshed?.ToString("dd MMM yyyy hh:mm tt") ?? "-";

        filters.TopDefects = await TryGetTopDefectsAsync(lineId, filters.WeekStart, cancellationToken);
        filters.ManpowerDays = await TryGetManpowerDaysAsync(lineId, filters.WeekStart, cancellationToken);

        return filters;
    }

    private async Task<List<(string Name, int Count)>> TryGetTopDefectsAsync(
        int shopId,
        DateTime weekStart,
        CancellationToken cancellationToken)
    {
        try
        {
            var rows = await _db.Database.SqlQuery<TopDefectProcedureRow>(
                    $"EXEC dbo.USP_Top5_Defects_CurrentMonth @ShopId = {shopId}, @WeekStart = {weekStart}")
                .ToListAsync(cancellationToken);

            return rows
                .Where(row => !string.IsNullOrWhiteSpace(row.Defect_Name))
                .Select(row => (row.Defect_Name, row.Defect_Count))
                .ToList();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Could not fetch top rework defects for shop {ShopId} and week {WeekStart}.",
                shopId,
                weekStart);
            return new List<(string Name, int Count)>();
        }
    }

    private async Task<List<ManpowerDayViewModel>> TryGetManpowerDaysAsync(
        int shopId,
        DateTime weekStart,
        CancellationToken cancellationToken)
    {
        try
        {
            var rows = await _db.Database.SqlQuery<ManpowerProcedureRow>(
                    $"EXEC DRONAADNSK_11012019.dbo.USP_ManpowerDeploymentByWeek @ShopId = {shopId}, @WeekStart = {weekStart}")
                .ToListAsync(cancellationToken);

            return rows
                .Where(row => row.ShiftName is "1st" or "2nd" or "3rd")
                .GroupBy(row => row.ShiftDate.Date)
                .OrderBy(group => group.Key)
                .Select(group =>
                {
                    var shifts = group
                        .GroupBy(row => row.ShiftName)
                        .ToDictionary(
                            shiftGroup => shiftGroup.Key,
                            shiftGroup =>
                            {
                                var associates = shiftGroup.Sum(row => row.AssociateCount);
                                var required = shiftGroup.Sum(row => row.RequiredWorkstations);
                                return new ManpowerShiftViewModel
                                {
                                    ShiftName = shiftGroup.Key,
                                    AssociateCount = associates,
                                    RequiredWorkstations = required,
                                    Deployment = CalculateDeployment(associates, required)
                                };
                            });
                    var orderedShifts = new[] { "1st", "2nd", "3rd" }
                        .Where(shifts.ContainsKey)
                        .Select(shiftName => shifts[shiftName])
                        .ToList();

                    var totalAssociates = orderedShifts.Sum(shift => shift.AssociateCount);
                    var totalRequired = orderedShifts.Sum(shift => shift.RequiredWorkstations);
                    return new ManpowerDayViewModel
                    {
                        Date = group.Key,
                        AssociateCount = totalAssociates,
                        RequiredWorkstations = totalRequired,
                        Deployment = CalculateDeployment(totalAssociates, totalRequired),
                        Shifts = orderedShifts
                    };
                })
                .ToList();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Could not fetch manpower deployment for shop {ShopId} and week {WeekStart}.",
                shopId,
                weekStart);
            return new List<ManpowerDayViewModel>();
        }
    }

    private static int CalculateDeployment(int associates, int requiredWorkstations) =>
        requiredWorkstations <= 0
            ? 0
            : Math.Min(100, (int)(associates * 100.0 / requiredWorkstations));

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
                WeekLabel = $"W{PmDates.FiscalWeekNumber(s.MeetingDate)}",
                Value = transactions.FirstOrDefault(t => t.SessionId == s.SessionId)?.WeekValue
            }).ToList()
        };
    }

    private sealed class TopDefectProcedureRow
    {
        public string Defect_Name { get; set; } = "";
        public int Defect_Count { get; set; }
    }

    private sealed class ManpowerProcedureRow
    {
        public string ShiftName { get; set; } = "";
        public DateTime ShiftDate { get; set; }
        public int AssociateCount { get; set; }
        public int RequiredWorkstations { get; set; }
    }
}
