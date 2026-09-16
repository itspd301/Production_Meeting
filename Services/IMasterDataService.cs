using ProductionMeeting.ViewModels.MasterData;

namespace ProductionMeeting.Services;

public enum MasterDataType
{
    Plant,
    Line,
    Shift,
    Department,
    Model,
    Unit,
    Indicator
}

public interface IMasterDataService
{
    Task<MasterDataIndexViewModel> GetIndexAsync(CancellationToken cancellationToken = default);

    Task<ServiceResult> SavePlantAsync(int? id, string code, string name, string userId, CancellationToken cancellationToken = default);
    Task<ServiceResult> SaveLineAsync(int? id, int plantId, string code, string name, string userId, CancellationToken cancellationToken = default);
    Task<ServiceResult> SaveShiftAsync(int? id, string name, TimeSpan? start, TimeSpan? end, string userId, CancellationToken cancellationToken = default);
    Task<ServiceResult> SaveDepartmentAsync(int? id, string name, string userId, CancellationToken cancellationToken = default);
    Task<ServiceResult> SaveModelAsync(int? id, string name, string userId, CancellationToken cancellationToken = default);
    Task<ServiceResult> SaveUnitAsync(int? id, string name, string userId, CancellationToken cancellationToken = default);
    Task<ServiceResult> SaveIndicatorAsync(int? id, string code, string name, string userId, CancellationToken cancellationToken = default);

    Task<ServiceResult> SetActiveAsync(MasterDataType type, int id, bool isActive, string userId, CancellationToken cancellationToken = default);
}
