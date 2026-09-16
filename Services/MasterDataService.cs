using Microsoft.EntityFrameworkCore;
using ProductionMeeting.Data;
using ProductionMeeting.Models;
using ProductionMeeting.ViewModels.MasterData;

namespace ProductionMeeting.Services;

public class MasterDataService : IMasterDataService
{
    private readonly ApplicationDbContext _db;

    public MasterDataService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<MasterDataIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default)
    {
        var vm = new MasterDataIndexViewModel
        {
            Plants = await _db.Plants.OrderBy(p => p.Name)
                .Select(p => new MasterRow { Id = p.PlantId, Code = p.Code, Name = p.Name, IsActive = p.IsActive })
                .ToListAsync(cancellationToken),

            Lines = await _db.ProductionLines.Include(l => l.Plant).OrderBy(l => l.Plant.Name).ThenBy(l => l.Name)
                .Select(l => new MasterRow { Id = l.LineId, Code = l.Code, Name = l.Name, IsActive = l.IsActive, PlantId = l.PlantId, PlantName = l.Plant.Name })
                .ToListAsync(cancellationToken),

            Shifts = await _db.Shifts.OrderBy(s => s.Name)
                .Select(s => new MasterRow
                {
                    Id = s.ShiftId,
                    Name = s.Name,
                    IsActive = s.IsActive,
                    StartTime = s.StartTime.HasValue ? s.StartTime.Value.ToString(@"hh\:mm") : null,
                    EndTime = s.EndTime.HasValue ? s.EndTime.Value.ToString(@"hh\:mm") : null
                })
                .ToListAsync(cancellationToken),

            Departments = await _db.Departments.OrderBy(d => d.Name)
                .Select(d => new MasterRow { Id = d.DepartmentId, Name = d.Name, IsActive = d.IsActive })
                .ToListAsync(cancellationToken),

            Models = await _db.ProductModels.OrderBy(m => m.Name)
                .Select(m => new MasterRow { Id = m.ModelId, Name = m.Name, IsActive = m.IsActive })
                .ToListAsync(cancellationToken),

            Units = await _db.Units.OrderBy(u => u.Name)
                .Select(u => new MasterRow { Id = u.UnitId, Name = u.Name, IsActive = u.IsActive })
                .ToListAsync(cancellationToken),

            Indicators = await _db.Indicators.OrderBy(i => i.IndicatorId)
                .Select(i => new MasterRow { Id = i.IndicatorId, Code = i.Code, Name = i.Name, IsActive = i.IsActive })
                .ToListAsync(cancellationToken),

            PlantOptions = await _db.Plants.Where(p => p.IsActive).OrderBy(p => p.Name)
                .Select(p => new ValueTuple<int, string>(p.PlantId, p.Name)).ToListAsync(cancellationToken)
        };

        return vm;
    }

    public async Task<ServiceResult> SavePlantAsync(int? id, string code, string name, string userId, CancellationToken cancellationToken = default)
    {
        if (await _db.Plants.AnyAsync(p => p.PlantId != id && p.Code == code, cancellationToken))
        {
            return ServiceResult.Fail("A plant with this code already exists.");
        }

        if (id.HasValue)
        {
            var plant = await _db.Plants.FirstOrDefaultAsync(p => p.PlantId == id, cancellationToken);
            if (plant == null) return ServiceResult.Fail("Plant not found.");
            plant.Code = code;
            plant.Name = name;
            plant.ModifiedBy = userId;
            plant.ModifiedDate = DateTime.UtcNow;
        }
        else
        {
            _db.Plants.Add(new Plant { Code = code, Name = name, CreatedBy = userId });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Plant saved successfully.");
    }

    public async Task<ServiceResult> SaveLineAsync(int? id, int plantId, string code, string name, string userId, CancellationToken cancellationToken = default)
    {
        if (await _db.ProductionLines.AnyAsync(l => l.LineId != id && l.PlantId == plantId && l.Code == code, cancellationToken))
        {
            return ServiceResult.Fail("A line with this code already exists for this plant.");
        }

        if (id.HasValue)
        {
            var line = await _db.ProductionLines.FirstOrDefaultAsync(l => l.LineId == id, cancellationToken);
            if (line == null) return ServiceResult.Fail("Line not found.");
            line.PlantId = plantId;
            line.Code = code;
            line.Name = name;
            line.ModifiedBy = userId;
            line.ModifiedDate = DateTime.UtcNow;
        }
        else
        {
            _db.ProductionLines.Add(new ProductionLine { PlantId = plantId, Code = code, Name = name, CreatedBy = userId });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Line saved successfully.");
    }

    public async Task<ServiceResult> SaveShiftAsync(int? id, string name, TimeSpan? start, TimeSpan? end, string userId, CancellationToken cancellationToken = default)
    {
        if (id.HasValue)
        {
            var shift = await _db.Shifts.FirstOrDefaultAsync(s => s.ShiftId == id, cancellationToken);
            if (shift == null) return ServiceResult.Fail("Shift not found.");
            shift.Name = name;
            shift.StartTime = start;
            shift.EndTime = end;
            shift.ModifiedBy = userId;
            shift.ModifiedDate = DateTime.UtcNow;
        }
        else
        {
            _db.Shifts.Add(new Shift { Name = name, StartTime = start, EndTime = end, CreatedBy = userId });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Shift saved successfully.");
    }

    public async Task<ServiceResult> SaveDepartmentAsync(int? id, string name, string userId, CancellationToken cancellationToken = default)
    {
        if (id.HasValue)
        {
            var dept = await _db.Departments.FirstOrDefaultAsync(d => d.DepartmentId == id, cancellationToken);
            if (dept == null) return ServiceResult.Fail("Department not found.");
            dept.Name = name;
            dept.ModifiedBy = userId;
            dept.ModifiedDate = DateTime.UtcNow;
        }
        else
        {
            _db.Departments.Add(new Department { Name = name, CreatedBy = userId });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Department saved successfully.");
    }

    public async Task<ServiceResult> SaveModelAsync(int? id, string name, string userId, CancellationToken cancellationToken = default)
    {
        if (id.HasValue)
        {
            var model = await _db.ProductModels.FirstOrDefaultAsync(m => m.ModelId == id, cancellationToken);
            if (model == null) return ServiceResult.Fail("Model not found.");
            model.Name = name;
            model.ModifiedBy = userId;
            model.ModifiedDate = DateTime.UtcNow;
        }
        else
        {
            _db.ProductModels.Add(new ProductModel { Name = name, CreatedBy = userId });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Model saved successfully.");
    }

    public async Task<ServiceResult> SaveUnitAsync(int? id, string name, string userId, CancellationToken cancellationToken = default)
    {
        if (id.HasValue)
        {
            var unit = await _db.Units.FirstOrDefaultAsync(u => u.UnitId == id, cancellationToken);
            if (unit == null) return ServiceResult.Fail("Unit not found.");
            unit.Name = name;
            unit.ModifiedBy = userId;
            unit.ModifiedDate = DateTime.UtcNow;
        }
        else
        {
            _db.Units.Add(new Unit { Name = name, CreatedBy = userId });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Unit saved successfully.");
    }

    public async Task<ServiceResult> SaveIndicatorAsync(int? id, string code, string name, string userId, CancellationToken cancellationToken = default)
    {
        if (id.HasValue)
        {
            var indicator = await _db.Indicators.FirstOrDefaultAsync(i => i.IndicatorId == id, cancellationToken);
            if (indicator == null) return ServiceResult.Fail("Indicator not found.");
            indicator.Code = code;
            indicator.Name = name;
            indicator.ModifiedBy = userId;
            indicator.ModifiedDate = DateTime.UtcNow;
        }
        else
        {
            _db.Indicators.Add(new Indicator { Code = code, Name = name, CreatedBy = userId });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok("Indicator saved successfully.");
    }

    public async Task<ServiceResult> SetActiveAsync(MasterDataType type, int id, bool isActive, string userId, CancellationToken cancellationToken = default)
    {
        AuditableEntity? entity = type switch
        {
            MasterDataType.Plant => await _db.Plants.FirstOrDefaultAsync(p => p.PlantId == id, cancellationToken),
            MasterDataType.Line => await _db.ProductionLines.FirstOrDefaultAsync(l => l.LineId == id, cancellationToken),
            MasterDataType.Shift => await _db.Shifts.FirstOrDefaultAsync(s => s.ShiftId == id, cancellationToken),
            MasterDataType.Department => await _db.Departments.FirstOrDefaultAsync(d => d.DepartmentId == id, cancellationToken),
            MasterDataType.Model => await _db.ProductModels.FirstOrDefaultAsync(m => m.ModelId == id, cancellationToken),
            MasterDataType.Unit => await _db.Units.FirstOrDefaultAsync(u => u.UnitId == id, cancellationToken),
            MasterDataType.Indicator => await _db.Indicators.FirstOrDefaultAsync(i => i.IndicatorId == id, cancellationToken),
            _ => null
        };

        if (entity == null)
        {
            return ServiceResult.Fail("Record not found.");
        }

        entity.IsActive = isActive;
        entity.ModifiedBy = userId;
        entity.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok(isActive ? "Activated." : "Deactivated.");
    }
}
