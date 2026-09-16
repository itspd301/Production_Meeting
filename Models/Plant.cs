namespace ProductionMeeting.Models;

public class Plant : AuditableEntity
{
    public int PlantId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    public ICollection<ProductionLine> ProductionLines { get; set; } = new List<ProductionLine>();
}
