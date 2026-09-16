using ProductionMeeting.Models;

namespace ProductionMeeting.Services;

public interface IUserAccessService
{
    // Admins get every active plant; everyone else gets only plants they've been granted access to.
    Task<List<Plant>> GetAccessiblePlantsAsync(string userId, bool isAdmin, CancellationToken cancellationToken = default);

    // Admins get every active line in the plant; everyone else gets only lines they've been granted
    // (a null LineId grant on the plant means "every line in that plant").
    Task<List<ProductionLine>> GetAccessibleLinesAsync(string userId, bool isAdmin, int plantId, CancellationToken cancellationToken = default);

    Task<bool> CanAccessLineAsync(string userId, bool isAdmin, int plantId, int lineId, CancellationToken cancellationToken = default);
}
