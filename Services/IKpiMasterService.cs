using ProductionMeeting.ViewModels.KpiMaster;

namespace ProductionMeeting.Services;

public interface IKpiMasterService
{
    Task<KpiMasterIndexViewModel> GetIndexAsync(KpiMasterIndexViewModel filters, CancellationToken cancellationToken = default);

    Task<KpiMasterFormViewModel> GetNewFormAsync(CancellationToken cancellationToken = default);

    Task<KpiMasterFormViewModel?> GetForEditAsync(int kpiId, CancellationToken cancellationToken = default);

    Task<ServiceResult> CreateAsync(KpiMasterFormViewModel form, string userId, CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateAsync(KpiMasterFormViewModel form, string userId, CancellationToken cancellationToken = default);

    Task<ServiceResult> SetActiveAsync(int kpiId, bool isActive, string userId, CancellationToken cancellationToken = default);

    Task<List<(int Id, string Name)>> GetLinesForPlantAsync(int plantId, CancellationToken cancellationToken = default);
}
