namespace ProductionMeeting.Models;

// Scopes which Plant/Line combinations a non-Admin user may see and edit.
// LineId == null means access to every line within that plant.
public class UserPlantLineAccess
{
    public int AccessId { get; set; }

    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;

    public int PlantId { get; set; }
    public Plant Plant { get; set; } = null!;

    public int? LineId { get; set; }
    public ProductionLine? Line { get; set; }
}
