namespace ProductionMeeting.Models;

// SQCDPSMO category: Safety, Quality, Cost, Delivery, Production, Sustainability, Morale, Others.
public class Indicator : AuditableEntity
{
    public int IndicatorId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
}
