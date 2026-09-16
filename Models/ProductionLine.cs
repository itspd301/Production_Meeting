namespace ProductionMeeting.Models;

public class ProductionLine : AuditableEntity
{
    public int LineId { get; set; }
    public int PlantId { get; set; }
    public Plant Plant { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
}
