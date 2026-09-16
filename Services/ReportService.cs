using Microsoft.EntityFrameworkCore;
using ProductionMeeting.Data;
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
