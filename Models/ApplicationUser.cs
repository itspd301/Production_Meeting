using Microsoft.AspNetCore.Identity;

namespace ProductionMeeting.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = null!;
    public string? Designation { get; set; }
    public bool IsActive { get; set; } = true;
}
