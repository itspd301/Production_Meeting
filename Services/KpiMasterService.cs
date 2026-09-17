using Microsoft.EntityFrameworkCore;
using ProductionMeeting.Data;
using ProductionMeeting.Models;
using ProductionMeeting.ViewModels.KpiMaster;

namespace ProductionMeeting.Services;

public class KpiMasterService : IKpiMasterService
{
    private readonly ApplicationDbContext _db;

    public KpiMasterService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<KpiMasterIndexViewModel> GetIndexAsync(KpiMasterIndexViewModel filters, CancellationToken cancellationToken = default)
    {
        filters.Plants = await _db.Plants.Where(p => p.IsActive).OrderBy(p => p.Name)
            .Select(p => new ValueTuple<int, string>(p.PlantId, p.Name)).ToListAsync(cancellationToken);

        if (filters.PlantId.HasValue)
        {
            filters.Lines = await GetLinesForPlantAsync(filters.PlantId.Value, cancellationToken);
        }

        var query = _db.KpiMasters
            .Include(k => k.Plant)
            .Include(k => k.Line)
            .Include(k => k.Indicator)
            .Include(k => k.Unit)
            .AsQueryable();

        if (filters.PlantId.HasValue)
        {
            query = query.Where(k => k.PlantId == filters.PlantId.Value);
        }
        if (filters.LineId.HasValue)
        {
            query = query.Where(k => k.LineId == filters.LineId.Value);
        }
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var term = filters.Search.Trim();
            query = query.Where(k => k.Description.Contains(term) || k.Indicator.Name.Contains(term));
        }

        filters.TotalCount = await query.CountAsync(cancellationToken);

        var page = Math.Max(1, filters.Page);
        var pageSize = filters.PageSize is > 0 and <= 100 ? filters.PageSize : 15;

        filters.Items = await query
            .OrderBy(k => k.Plant.Name).ThenBy(k => k.Line.Name).ThenBy(k => k.Indicator.IndicatorId).ThenBy(k => k.DisplayOrder)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(k => new KpiMasterListItemViewModel
            {
                KpiId = k.KpiId,
                PlantName = k.Plant.Name,
                LineName = k.Line.Name,
                IndicatorCode = k.Indicator.Code,
                IndicatorName = k.Indicator.Name,
                UnitName = k.Unit.Name,
                Description = k.Description,
                DisplayOrder = k.DisplayOrder,
                IsActive = k.IsActive,
                Source = k.Source
            })
            .ToListAsync(cancellationToken);

        filters.Page = page;
        filters.PageSize = pageSize;

        return filters;
    }

    public async Task<KpiMasterFormViewModel> GetNewFormAsync(CancellationToken cancellationToken = default)
    {
        return new KpiMasterFormViewModel
        {
            IsActive = true,
            Plants = await GetPlantOptionsAsync(cancellationToken),
            Indicators = await GetIndicatorOptionsAsync(cancellationToken),
            Units = await GetUnitOptionsAsync(cancellationToken)
        };
    }

    public async Task<KpiMasterFormViewModel?> GetForEditAsync(int kpiId, CancellationToken cancellationToken = default)
    {
        var kpi = await _db.KpiMasters.FirstOrDefaultAsync(k => k.KpiId == kpiId, cancellationToken);
        if (kpi == null)
        {
            return null;
        }

        return new KpiMasterFormViewModel
        {
            KpiId = kpi.KpiId,
            PlantId = kpi.PlantId,
            LineId = kpi.LineId,
            IndicatorId = kpi.IndicatorId,
            UnitId = kpi.UnitId,
            Description = kpi.Description,
            DisplayOrder = kpi.DisplayOrder,
            IsActive = kpi.IsActive,
            Source = kpi.Source ?? Helpers.KpiSources.Manual,
            LowerIsBetter = kpi.LowerIsBetter,
            Plants = await GetPlantOptionsAsync(cancellationToken),
            Lines = await GetLinesForPlantAsync(kpi.PlantId, cancellationToken),
            Indicators = await GetIndicatorOptionsAsync(cancellationToken),
            Units = await GetUnitOptionsAsync(cancellationToken)
        };
    }

    public async Task<ServiceResult> CreateAsync(KpiMasterFormViewModel form, string userId, CancellationToken cancellationToken = default)
    {
        var duplicate = await _db.KpiMasters.AnyAsync(k =>
            k.PlantId == form.PlantId && k.LineId == form.LineId &&
            k.IndicatorId == form.IndicatorId && k.Description == form.Description, cancellationToken);

        if (duplicate)
        {
            return ServiceResult.Fail("A KPI with this description already exists for this plant/line/indicator.");
        }

        _db.KpiMasters.Add(new Models.KpiMaster
        {
            PlantId = form.PlantId,
            LineId = form.LineId,
            IndicatorId = form.IndicatorId,
            UnitId = form.UnitId,
            Description = form.Description.Trim(),
            DisplayOrder = form.DisplayOrder,
            IsActive = form.IsActive,
            Source = form.Source,
            LowerIsBetter = form.LowerIsBetter,
            CreatedBy = userId
        });

        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("KPI created successfully.");
    }

    public async Task<ServiceResult> UpdateAsync(KpiMasterFormViewModel form, string userId, CancellationToken cancellationToken = default)
    {
        if (form.KpiId == null)
        {
            return ServiceResult.Fail("Invalid KPI.");
        }

        var kpi = await _db.KpiMasters.FirstOrDefaultAsync(k => k.KpiId == form.KpiId, cancellationToken);
        if (kpi == null)
        {
            return ServiceResult.Fail("KPI not found.");
        }

        var duplicate = await _db.KpiMasters.AnyAsync(k =>
            k.KpiId != form.KpiId && k.PlantId == form.PlantId && k.LineId == form.LineId &&
            k.IndicatorId == form.IndicatorId && k.Description == form.Description, cancellationToken);

        if (duplicate)
        {
            return ServiceResult.Fail("A KPI with this description already exists for this plant/line/indicator.");
        }

        kpi.PlantId = form.PlantId;
        kpi.LineId = form.LineId;
        kpi.IndicatorId = form.IndicatorId;
        kpi.UnitId = form.UnitId;
        kpi.Description = form.Description.Trim();
        kpi.DisplayOrder = form.DisplayOrder;
        kpi.IsActive = form.IsActive;
        kpi.Source = form.Source;
        kpi.LowerIsBetter = form.LowerIsBetter;
        kpi.ModifiedBy = userId;
        kpi.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("KPI updated successfully.");
    }

    public async Task<ServiceResult> SetActiveAsync(int kpiId, bool isActive, string userId, CancellationToken cancellationToken = default)
    {
        var kpi = await _db.KpiMasters.FirstOrDefaultAsync(k => k.KpiId == kpiId, cancellationToken);
        if (kpi == null)
        {
            return ServiceResult.Fail("KPI not found.");
        }

        kpi.IsActive = isActive;
        kpi.ModifiedBy = userId;
        kpi.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok(isActive ? "KPI activated." : "KPI deactivated.");
    }

    public async Task<List<(int Id, string Name)>> GetLinesForPlantAsync(int plantId, CancellationToken cancellationToken = default)
    {
        return await _db.ProductionLines.Where(l => l.IsActive && l.PlantId == plantId).OrderBy(l => l.Name)
            .Select(l => new ValueTuple<int, string>(l.LineId, l.Name)).ToListAsync(cancellationToken);
    }

    private async Task<List<(int Id, string Name)>> GetPlantOptionsAsync(CancellationToken cancellationToken)
    {
        return await _db.Plants.Where(p => p.IsActive).OrderBy(p => p.Name)
            .Select(p => new ValueTuple<int, string>(p.PlantId, p.Name)).ToListAsync(cancellationToken);
    }

    private async Task<List<(int Id, string Code, string Name)>> GetIndicatorOptionsAsync(CancellationToken cancellationToken)
    {
        return await _db.Indicators.Where(i => i.IsActive).OrderBy(i => i.IndicatorId)
            .Select(i => new ValueTuple<int, string, string>(i.IndicatorId, i.Code, i.Name)).ToListAsync(cancellationToken);
    }

    private async Task<List<(int Id, string Name)>> GetUnitOptionsAsync(CancellationToken cancellationToken)
    {
        return await _db.Units.Where(u => u.IsActive).OrderBy(u => u.Name)
            .Select(u => new ValueTuple<int, string>(u.UnitId, u.Name)).ToListAsync(cancellationToken);
    }
}
