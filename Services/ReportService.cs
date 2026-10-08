using Microsoft.EntityFrameworkCore;
using ProductionMeeting.Data;
using ProductionMeeting.Helpers;
using ProductionMeeting.Models;
using ProductionMeeting.ViewModels.Reports;

namespace ProductionMeeting.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;
    private readonly IUserAccessService _accessService;

    public ReportService(ApplicationDbContext db, IUserAccessService accessService)
    {
        _db = db;
        _accessService = accessService;
    }

    public async Task<ReportIndexViewModel> GetReportAsync(ReportIndexViewModel filters, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        await PopulateFiltersAsync(filters, userId, isAdmin, cancellationToken);

        var query = await BuildQueryAsync(filters, userId, isAdmin, cancellationToken);

        filters.TotalCount = await query.CountAsync(cancellationToken);

        var page = Math.Max(1, filters.Page);
        var pageSize = filters.PageSize is > 0 and <= 200 ? filters.PageSize : 20;

        filters.Rows = await query
            .OrderByDescending(t => t.Session.MeetingDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new ReportRowViewModel
            {
                MeetingDate = t.Session.MeetingDate,
                PlantName = t.Session.Plant.Name,
                LineName = t.Session.Line.Name,
                IndicatorCode = t.Kpi.Indicator.Code,
                IndicatorName = t.Kpi.Indicator.Name,
                KpiDescription = t.Kpi.Description,
                UnitName = t.Kpi.Unit.Name,
                ModelName = t.Model != null ? t.Model.Name : null,
                F26Value = t.F26Value,
                F27Value = t.F27Value,
                WeekValue = t.WeekValue,
                MonthCumValue = t.MonthCumValue,
                YtdValue = t.YtdValue,
                Remarks = t.Remarks
            })
            .ToListAsync(cancellationToken);

        filters.Page = page;
        filters.PageSize = pageSize;

        return filters;
    }

    public async Task<List<ReportRowViewModel>> GetReportRowsForExportAsync(ReportIndexViewModel filters, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var query = await BuildQueryAsync(filters, userId, isAdmin, cancellationToken);

        return await query
            .OrderByDescending(t => t.Session.MeetingDate)
            .Select(t => new ReportRowViewModel
            {
                MeetingDate = t.Session.MeetingDate,
                PlantName = t.Session.Plant.Name,
                LineName = t.Session.Line.Name,
                IndicatorCode = t.Kpi.Indicator.Code,
                IndicatorName = t.Kpi.Indicator.Name,
                KpiDescription = t.Kpi.Description,
                UnitName = t.Kpi.Unit.Name,
                ModelName = t.Model != null ? t.Model.Name : null,
                F26Value = t.F26Value,
                F27Value = t.F27Value,
                WeekValue = t.WeekValue,
                MonthCumValue = t.MonthCumValue,
                YtdValue = t.YtdValue,
                Remarks = t.Remarks
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<WeeklyReportViewModel> GetWeeklyReportAsync(
        WeeklyReportViewModel filters,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        filters.Plants = (await _accessService.GetAccessiblePlantsAsync(userId, isAdmin, cancellationToken))
            .Select(plant => (plant.PlantId, plant.Name))
            .ToList();

        if (filters.PlantId.HasValue &&
            !filters.Plants.Any(plant => plant.Id == filters.PlantId.Value))
        {
            filters.PlantId = null;
            filters.LineId = null;
        }

        if (filters.PlantId.HasValue)
        {
            filters.Lines = (await _accessService.GetAccessibleLinesAsync(
                    userId, isAdmin, filters.PlantId.Value, cancellationToken))
                .Select(line => (line.LineId, line.Name))
                .ToList();

            if (filters.LineId.HasValue &&
                !filters.Lines.Any(line => line.Id == filters.LineId.Value))
            {
                filters.LineId = null;
            }
        }

        filters.Indicators = await _db.Indicators
            .Where(indicator => indicator.IsActive)
            .OrderBy(indicator => indicator.IndicatorId)
            .Select(indicator => new ValueTuple<int, string, string>(
                indicator.IndicatorId, indicator.Code, indicator.Name))
            .ToListAsync(cancellationToken);

        var today = DateTime.Today;
        var fiscalYearEnd = PmDates.FiscalYearEndYearOfWeek(today);
        var defaultStart = PmDates.GetMondayOfWeek(new DateTime(fiscalYearEnd - 1, 4, 1));
        var defaultEnd = PmDates.GetMondayOfWeek(today);
        filters.StartWeekStart = PmDates.GetMondayOfWeek(
            PmDates.FromWeekInputValue(filters.StartWeek) ?? defaultStart);
        filters.EndWeekStart = PmDates.GetMondayOfWeek(
            PmDates.FromWeekInputValue(filters.EndWeek) ?? defaultEnd);
        filters.StartWeek = PmDates.ToWeekInputValue(filters.StartWeekStart);
        filters.EndWeek = PmDates.ToWeekInputValue(filters.EndWeekStart);

        if (filters.EndWeekStart < filters.StartWeekStart)
        {
            filters.RangeError = "End week must be on or after start week.";
            return filters;
        }

        var weekCount = (int)((filters.EndWeekStart - filters.StartWeekStart).TotalDays / 7) + 1;
        if (weekCount > 52)
        {
            filters.RangeError = "Select a range of 52 weeks or fewer.";
            return filters;
        }

        var weekStarts = Enumerable.Range(0, weekCount)
            .Select(offset => filters.StartWeekStart.AddDays(offset * 7))
            .ToList();
        filters.Weeks = weekStarts
            .Select(weekStart => (weekStart, PmDates.WeekLabel(weekStart)))
            .ToList();

        var accessiblePlantIds = filters.Plants.Select(plant => plant.Id).ToList();
        var accessibleLineIds = new List<int>();
        var allAccessibleLines = new List<(int Id, string Name)>();
        foreach (var plantId in accessiblePlantIds)
        {
            if (filters.PlantId.HasValue && plantId != filters.PlantId.Value)
            {
                continue;
            }

            List<int> lineIds;
            if (filters.PlantId.HasValue)
            {
                lineIds = filters.Lines.Select(line => line.Id).ToList();
            }
            else
            {
                var lines = await _accessService.GetAccessibleLinesAsync(
                    userId, isAdmin, plantId, cancellationToken);
                lineIds = lines.Select(line => line.LineId).ToList();
                allAccessibleLines.AddRange(lines.Select(line => (line.LineId, line.Name)));
            }

            accessibleLineIds.AddRange(lineIds);
        }

        if (!filters.PlantId.HasValue)
        {
            filters.Lines = allAccessibleLines.Distinct().ToList();
        }
        if (filters.LineId.HasValue && !accessibleLineIds.Contains(filters.LineId.Value))
        {
            filters.LineId = null;
        }

        var kpis = await _db.KpiMasters
            .Include(kpi => kpi.Plant)
            .Include(kpi => kpi.Line)
            .Include(kpi => kpi.Indicator)
            .Include(kpi => kpi.Unit)
            .Where(kpi => kpi.IsActive
                && accessiblePlantIds.Contains(kpi.PlantId)
                && accessibleLineIds.Contains(kpi.LineId))
            .Where(kpi => !filters.PlantId.HasValue || kpi.PlantId == filters.PlantId.Value)
            .Where(kpi => !filters.LineId.HasValue || kpi.LineId == filters.LineId.Value)
            .Where(kpi => !filters.IndicatorId.HasValue || kpi.IndicatorId == filters.IndicatorId.Value)
            .OrderBy(kpi => kpi.Plant.Name)
            .ThenBy(kpi => kpi.Line.Name)
            .ThenBy(kpi => kpi.Indicator.IndicatorId)
            .ThenBy(kpi => kpi.DisplayOrder)
            .ToListAsync(cancellationToken);

        var rangeEndExclusive = filters.EndWeekStart.AddDays(7);
        var sessions = await _db.MeetingSessions
            .Where(session => accessiblePlantIds.Contains(session.PlantId)
                && accessibleLineIds.Contains(session.LineId)
                && session.MeetingDate >= filters.StartWeekStart.AddDays(-7)
                && session.MeetingDate < rangeEndExclusive)
            .Where(session => !filters.PlantId.HasValue || session.PlantId == filters.PlantId.Value)
            .Where(session => !filters.LineId.HasValue || session.LineId == filters.LineId.Value)
            .ToListAsync(cancellationToken);

        var sessionIds = sessions.Select(session => session.SessionId).ToList();
        var kpiIds = kpis.Select(kpi => kpi.KpiId).ToList();
        var transactions = await _db.KpiTransactions
            .Where(transaction => sessionIds.Contains(transaction.SessionId)
                && kpiIds.Contains(transaction.KpiId))
            .ToListAsync(cancellationToken);

        var sessionLookup = sessions.ToDictionary(
            session => (session.PlantId, session.LineId, PmDates.GetMondayOfWeek(session.MeetingDate)),
            session => session.SessionId);
        var transactionLookup = transactions
            .GroupBy(transaction => (transaction.KpiId, transaction.SessionId))
            .ToDictionary(group => group.Key, group => group.First());

        KpiTransaction? GetTransaction(KpiMaster kpi, DateTime requestedWeek)
        {
            if (!sessionLookup.TryGetValue((kpi.PlantId, kpi.LineId, requestedWeek), out var sessionId))
            {
                return null;
            }

            return transactionLookup.TryGetValue((kpi.KpiId, sessionId), out var transaction)
                ? transaction
                : null;
        }

        filters.Rows = kpis.Select(kpi =>
        {
            return new WeeklyReportRowViewModel
            {
                KpiId = kpi.KpiId,
                PlantName = kpi.Plant.Name,
                LineName = kpi.Line.Name,
                IndicatorCode = kpi.Indicator.Code,
                KpiDescription = kpi.Description,
                UnitName = kpi.Unit.Name,
                Values = weekStarts.Select(weekStart =>
                {
                    var current = GetTransaction(kpi, weekStart);
                    var previous = GetTransaction(kpi, weekStart.AddDays(-7));
                    var target = current?.F27Value ?? previous?.F27Value ?? kpi.F27Value;
                    var (status, _) = KpiStatusCalculator.Compute(kpi.LowerIsBetter, current?.WeekValue, target);

                    return new WeeklyReportValueViewModel
                    {
                        Value = current?.WeekValue,
                        Target = target,
                        Change = current?.WeekValue is decimal currentValue
                            && previous?.WeekValue is decimal previousValue
                            ? currentValue - previousValue
                            : null,
                        Status = status,
                        Comparison = GetWeekComparison(kpi.LowerIsBetter, current?.WeekValue, previous?.WeekValue)
                    };
                }).ToList()
            };
        }).ToList();

        filters.TotalCount = filters.Rows.Count;
        var latestWeekValues = filters.Rows.Select(row => row.Values[^1]).ToList();
        filters.EnteredCount = latestWeekValues.Count(value => value.Value.HasValue);
        filters.GreenCount = latestWeekValues.Count(value => value.Status == "Green");
        filters.AmberCount = latestWeekValues.Count(value => value.Status == "Amber");
        filters.RedCount = latestWeekValues.Count(value => value.Status == "Red");
        filters.ImprovedCount = filters.Rows.Sum(row => row.Values.Count(value => value.Comparison == "Improved"));
        filters.DeclinedCount = filters.Rows.Sum(row => row.Values.Count(value => value.Comparison == "Declined"));

        return filters;
    }

    private static string GetWeekComparison(bool lowerIsBetter, decimal? current, decimal? previous)
    {
        if (!current.HasValue)
        {
            return "No selected value";
        }

        if (!previous.HasValue)
        {
            return "No previous value";
        }

        if (current.Value == previous.Value)
        {
            return "Unchanged";
        }

        var improved = lowerIsBetter ? current.Value < previous.Value : current.Value > previous.Value;
        return improved ? "Improved" : "Declined";
    }

    private async Task PopulateFiltersAsync(ReportIndexViewModel filters, string userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var plants = await _accessService.GetAccessiblePlantsAsync(userId, isAdmin, cancellationToken);
        filters.Plants = plants.Select(p => (p.PlantId, p.Name)).ToList();

        if (filters.PlantId.HasValue)
        {
            var lines = await _accessService.GetAccessibleLinesAsync(userId, isAdmin, filters.PlantId.Value, cancellationToken);
            filters.Lines = lines.Select(l => (l.LineId, l.Name)).ToList();
        }

        filters.Indicators = await _db.Indicators.Where(i => i.IsActive).OrderBy(i => i.IndicatorId)
            .Select(i => new ValueTuple<int, string, string>(i.IndicatorId, i.Code, i.Name)).ToListAsync(cancellationToken);
    }

    private async Task<IQueryable<Models.KpiTransaction>> BuildQueryAsync(ReportIndexViewModel filters, string userId, bool isAdmin, CancellationToken cancellationToken)
    {
        var accessiblePlantIds = (await _accessService.GetAccessiblePlantsAsync(userId, isAdmin, cancellationToken))
            .Select(p => p.PlantId).ToList();

        var query = _db.KpiTransactions
            .Include(t => t.Session).ThenInclude(s => s.Plant)
            .Include(t => t.Session).ThenInclude(s => s.Line)
            .Include(t => t.Kpi).ThenInclude(k => k.Indicator)
            .Include(t => t.Kpi).ThenInclude(k => k.Unit)
            .Include(t => t.Model)
            .Where(t => accessiblePlantIds.Contains(t.Session.PlantId));

        if (filters.PlantId.HasValue)
        {
            query = query.Where(t => t.Session.PlantId == filters.PlantId.Value);
        }
        if (filters.LineId.HasValue)
        {
            query = query.Where(t => t.Session.LineId == filters.LineId.Value);
        }
        if (filters.IndicatorId.HasValue)
        {
            query = query.Where(t => t.Kpi.IndicatorId == filters.IndicatorId.Value);
        }
        if (filters.FromDate.HasValue)
        {
            query = query.Where(t => t.Session.MeetingDate >= filters.FromDate.Value.Date);
        }
        if (filters.ToDate.HasValue)
        {
            query = query.Where(t => t.Session.MeetingDate <= filters.ToDate.Value.Date);
        }

        return query;
    }
}
