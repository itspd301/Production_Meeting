using ProductionMeeting.ViewModels.UserManagement;

namespace ProductionMeeting.Services;

public interface IUserManagementService
{
    Task<List<UserListItemViewModel>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<UserFormViewModel> GetNewUserFormAsync(CancellationToken cancellationToken = default);

    Task<UserFormViewModel?> GetUserForEditAsync(string userId, CancellationToken cancellationToken = default);

    Task<ServiceResult> CreateUserAsync(UserFormViewModel form, CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateUserAsync(UserFormViewModel form, CancellationToken cancellationToken = default);

    Task<ServiceResult> SetActiveAsync(string userId, bool isActive, CancellationToken cancellationToken = default);
}
