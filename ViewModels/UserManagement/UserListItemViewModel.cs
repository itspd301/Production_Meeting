namespace ProductionMeeting.ViewModels.UserManagement;

public class UserListItemViewModel
{
    public string Id { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Role { get; set; } = null!;
    public bool IsActive { get; set; }
    public string AccessSummary { get; set; } = null!;
}
