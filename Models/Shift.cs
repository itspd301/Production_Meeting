namespace ProductionMeeting.Models;

public class Shift : AuditableEntity
{
    public int ShiftId { get; set; }
    public string Name { get; set; } = null!;
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
}
