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

        ApplyStaticPlaceholders(filters);

        return filters;
    }

    // Everything below is placeholder/sample data until it's wired up to a stored
    // procedure (same pattern as DashboardService.TryGetScalarAsync for the Straight Pass
    // Ratio / Traceability cards). Replace each assignment with a real SP-backed value as
    // those become available - group by group, this can be swapped out incrementally.
    private static void ApplyStaticPlaceholders(ExecutiveDashboardViewModel filters)
    {
        filters.Groups =
        [
            new ExecutiveIndicatorGroupViewModel
            {
                IndicatorCode = "S",
                IndicatorName = "Safety",
                Kpis =
                [
                    Card(1, "First Aid", "Nos", 1m, 2m, 2m, 3m, 9m, lowerIsBetter: true),
                    Card(2, "Near Miss incidence", "Nos", 3m, 5m, 4m, 10m, 28m, lowerIsBetter: true),
                    Card(3, "Fire Incidence", "Nos", 0m, 0m, 0m, 0m, 1m, lowerIsBetter: true)
                ]
            },
            new ExecutiveIndicatorGroupViewModel
            {
                IndicatorCode = "Q",
                IndicatorName = "Quality",
                Kpis =
                [
                    Card(4, "Offline Rework RPT XUV3XO", "Nos", 245m, 100m, 210m, 620m, 1840m, lowerIsBetter: true),
                    Card(5, "Offline Rework RPT XUV400", "Nos", 180m, 100m, 175m, 540m, 1490m, lowerIsBetter: true),
                    Card(8, "Process wise Zero Defect Stages *", "%", 92.5m, 95m, 91m, 92m, 90.5m, lowerIsBetter: false)
                ]
            },
            new ExecutiveIndicatorGroupViewModel
            {
                IndicatorCode = "C",
                IndicatorName = "Cost",
                Kpis =
                [
                    Card(9, "Scrap Cost Process", "Rs./Veh", 420m, 380m, 410m, 405m, 398m, lowerIsBetter: true),
                    Card(10, "Repairs & Maint. Cost Saving", "Rs. Lacs", 3.4m, 3.0m, 3.1m, 9.8m, 34.2m, lowerIsBetter: false),
                    Card(12, "Traceability", "%", 99.2m, 99.5m, 99.0m, 99.1m, 98.9m, lowerIsBetter: false)
                ]
            },
            new ExecutiveIndicatorGroupViewModel
            {
                IndicatorCode = "D",
                IndicatorName = "Delivery",
                Kpis =
                [
                    Card(13, "Schedule Adh.", "%", 96.8m, 98m, 95.5m, 96.2m, 95.8m, lowerIsBetter: false),
                    Card(14, "A Rank Breakdown", "Nos", 2m, 1m, 3m, 6m, 22m, lowerIsBetter: true),
                    Card(16, "Straight Pass Ratio", "%", 87.4m, 90m, 86m, 87m, 85.6m, lowerIsBetter: false)
                ]
            },
            new ExecutiveIndicatorGroupViewModel
            {
                IndicatorCode = "P",
                IndicatorName = "Production",
                Kpis =
                [
                    Card(18, "Eq.veh/man/year", "Nos", 42.3m, 45m, 41.8m, 42.0m, 41.5m, lowerIsBetter: false)
                ]
            },
            new ExecutiveIndicatorGroupViewModel
            {
                IndicatorCode = "SUST",
                IndicatorName = "Sustainability",
                Kpis =
                [
                    Card(19, "Water", "Ltrs/Veh", 2850m, 3000m, 2900m, 2870m, 2920m, lowerIsBetter: true),
                    Card(20, "Power", "Units/Veh", 615m, 600m, 620m, 610m, 618m, lowerIsBetter: true)
                ]
            },
            new ExecutiveIndicatorGroupViewModel
            {
                IndicatorCode = "M",
                IndicatorName = "Morale",
                Kpis =
                [
                    Card(21, "Recognition of Associates through JIGYASA Portal", "Nos", 18m, 15m, 14m, 46m, 152m, lowerIsBetter: false),
                    Card(22, "Ergonomy Status", "No. of Stages", 24m, 26m, 23m, 24m, 23m, lowerIsBetter: false)
                ]
            },
            new ExecutiveIndicatorGroupViewModel
            {
                IndicatorCode = "O",
                IndicatorName = "Others",
                Kpis =
                [
                    Card(24, "CCTV Camera Working Status", "Nos", 46m, 48m, 47m, 46m, 46m, lowerIsBetter: false)
                ]
            }
        ];

        filters.TotalCount = filters.Groups.Sum(g => g.Kpis.Count);
        filters.EnteredCount = filters.Groups.Sum(g => g.Kpis.Count(c => c.Value.HasValue));

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

    private static ExecutiveKpiCardViewModel Card(int kpiId, string description, string unitName, decimal value, decimal target, decimal previousValue, decimal monthCumValue, decimal ytdValue, bool lowerIsBetter)
    {
        var (status, variance) = KpiStatusCalculator.Compute(lowerIsBetter, value, target);
        return new ExecutiveKpiCardViewModel
        {
            KpiId = kpiId,
            Description = description,
            UnitName = unitName,
            Value = value,
            Target = target,
            PreviousValue = previousValue,
            MonthCumValue = monthCumValue,
            YtdValue = ytdValue,
            Status = status,
            Variance = variance
        };
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
