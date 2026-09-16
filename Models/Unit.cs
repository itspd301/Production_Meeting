namespace ProductionMeeting.Models;

public class Unit : AuditableEntity
{
    public int UnitId { get; set; }
    public string Name { get; set; } = null!;
}
