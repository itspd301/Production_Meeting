using System.ComponentModel.DataAnnotations;

namespace ProductionMeeting.ViewModels.UserManagement;

public class UserFormViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Windows account is required, e.g. DOMAIN\\username")]
    [Display(Name = "Windows Account")]
    public string UserName { get; set; } = null!;

    [Required(ErrorMessage = "Full name is required")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Role is required")]
    public string Role { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    // Plant access: a null LineId in the pair means "every line in that plant".
    public List<PlantAccessRow> PlantAccess { get; set; } = new();

    public List<string> AvailableRoles { get; set; } = new();
    public List<(int Id, string Name)> AvailablePlants { get; set; } = new();
}

public class PlantAccessRow
{
    public int PlantId { get; set; }
    public string PlantName { get; set; } = null!;
    public int? LineId { get; set; }
    public string? LineName { get; set; }
}
