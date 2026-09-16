using Microsoft.EntityFrameworkCore;
using ProductionMeeting.Data;
using ProductionMeeting.Models;

namespace ProductionMeeting.Services;

public class UserAccessService : IUserAccessService
{
    private readonly ApplicationDbContext _db;

    public UserAccessService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<Plant>> GetAccessiblePlantsAsync(string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        if (isAdmin)
        {
            return await _db.Plants.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync(cancellationToken);
        }

        var plantIds = await _db.UserPlantLineAccesses
            .Where(a => a.UserId == userId)
            .Select(a => a.PlantId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return await _db.Plants
            .Where(p => p.IsActive && plantIds.Contains(p.PlantId))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ProductionLine>> GetAccessibleLinesAsync(string userId, bool isAdmin, int plantId, CancellationToken cancellationToken = default)
    {
        var lineQuery = _db.ProductionLines.Where(l => l.IsActive && l.PlantId == plantId);

        if (isAdmin)
        {
            return await lineQuery.OrderBy(l => l.Name).ToListAsync(cancellationToken);
        }

        var access = await _db.UserPlantLineAccesses
            .Where(a => a.UserId == userId && a.PlantId == plantId)
            .ToListAsync(cancellationToken);

        if (access.Any(a => a.LineId == null))
        {
            return await lineQuery.OrderBy(l => l.Name).ToListAsync(cancellationToken);
        }

        var lineIds = access.Where(a => a.LineId != null).Select(a => a.LineId!.Value).ToList();
        return await lineQuery.Where(l => lineIds.Contains(l.LineId)).OrderBy(l => l.Name).ToListAsync(cancellationToken);
    }

    public async Task<bool> CanAccessLineAsync(string userId, bool isAdmin, int plantId, int lineId, CancellationToken cancellationToken = default)
    {
        if (isAdmin)
        {
            return true;
        }

        return await _db.UserPlantLineAccesses.AnyAsync(
            a => a.UserId == userId && a.PlantId == plantId && (a.LineId == null || a.LineId == lineId),
            cancellationToken);
    }
}
