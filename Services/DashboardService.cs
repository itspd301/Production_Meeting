using Microsoft.EntityFrameworkCore;
using ProductionMeeting.Data;
using ProductionMeeting.Helpers;
using ProductionMeeting.Models;
using ProductionMeeting.ViewModels;

namespace ProductionMeeting.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _db;
    private readonly IUserAccessService _accessService;

    public DashboardService(ApplicationDbContext db, IUserAccessService accessService)
    {
        _db = db;
        _accessService = accessService;
    }

    public async Task<DashboardViewModel> GetDashboardAsync(DashboardViewModel filters, string userId, bool isAdmin, CancellationToken cancellationToken = default)
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

        filters.WeekStart = PmDates.FromWeekInputValue(filters.Week) ?? GetMonday(DateTime.Today);
        filters.Week = PmDates.ToWeekInputValue(filters.WeekStart);

        filters.HasPlantAndShop = filters.PlantId.HasValue && filters.LineId.HasValue;
        if (!filters.HasPlantAndShop)
        {
            return filters;
        }

        var session = await _db.MeetingSessions
            .FirstOrDefaultAsync(s => s.PlantId == filters.PlantId && s.LineId == filters.LineId && s.MeetingDate == filters.WeekStart, cancellationToken);

        filters.SessionId = session?.SessionId;
        filters.SessionStatus = session?.Status == MeetingSessionStatus.Completed ? "Completed" : "Draft";

        var kpis = await _db.KpiMasters
            .Include(k => k.Indicator)
            .Include(k => k.Unit)
            .Where(k => k.IsActive && k.PlantId == filters.PlantId && k.LineId == filters.LineId)
            .OrderBy(k => k.Indicator.IndicatorId).ThenBy(k => k.DisplayOrder)
            .ToListAsync(cancellationToken);

        var transactions = session == null
            ? new List<KpiTransaction>()
            : await _db.KpiTransactions.Where(t => t.SessionId == session.SessionId).ToListAsync(cancellationToken);

        filters.Rows = kpis.Select(k =>
        {
            var tx = transactions.FirstOrDefault(t => t.KpiId == k.KpiId);
            return BuildRow(k, tx);
        }).ToList();

        filters.Categories = kpis
            .Select(k => (k.Indicator.Code, k.Indicator.Name))
            .Distinct()
            .OrderBy(c => c.Code)
            .ToList();

        filters.TotalCount = filters.Rows.Count;
        filters.EnteredCount = filters.Rows.Count(r => r.WeekValue.HasValue);
        filters.AttentionRequired = filters.Rows.Where(r => r.Status == "Red").Take(6).ToList();

        filters.Cards = BuildCards(filters);

        return filters;
    }

    private static DashboardKpiRowViewModel BuildRow(KpiMaster k, KpiTransaction? tx)
    {
        var row = new DashboardKpiRowViewModel
        {
            KpiId = k.KpiId,
            IndicatorCode = k.Indicator.Code,
            IndicatorName = k.Indicator.Name,
            Description = k.Description,
            UnitName = k.Unit.Name,
            Target = tx?.F27Value,
            WeekValue = tx?.WeekValue,
            MonthCumValue = tx?.MonthCumValue,
            YtdValue = tx?.YtdValue,
            Remarks = tx?.Remarks
        };

        if (!row.WeekValue.HasValue)
        {
            row.Status = "Pending";
        }
        else if (!row.Target.HasValue)
        {
            row.Status = "NoTarget";
        }
        else
        {
            row.Variance = row.WeekValue - row.Target;
            var good = k.LowerIsBetter ? row.WeekValue <= row.Target : row.WeekValue >= row.Target;

            if (good)
            {
                row.Status = "Green";
            }
            else
            {
                var tolerance = Math.Abs(row.Target.Value) * 0.05m;
                row.Status = Math.Abs(row.Variance.Value) <= tolerance ? "Amber" : "Red";
            }
        }

        return row;
    }

    private static List<DashboardCardViewModel> BuildCards(DashboardViewModel filters)
    {
        var cards = new List<DashboardCardViewModel>
        {
            new()
            {
                Label = "This Week's Progress",
                ValueDisplay = $"{filters.EnteredCount}/{filters.TotalCount}",
                Status = filters.TotalCount > 0 && filters.EnteredCount == filters.TotalCount ? "good" : "warn",
                SubText = "Indicators entered"
            },
            new()
            {
                Label = "Issues Flagged",
                ValueDisplay = filters.Rows.Count(r => r.Status == "Red").ToString(),
                Status = filters.Rows.Any(r => r.Status == "Red") ? "bad" : "good",
                SubText = "Red-status KPIs"
            },
            new()
            {
                Label = "Meeting Status",
                ValueDisplay = filters.SessionStatus,
                Status = filters.SessionStatus == "Completed" ? "good" : "warn",
                SubText = PmDates.WeekLabel(filters.WeekStart)
            }
        };

        return cards;
    }

    private static DateTime GetMonday(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }
}
