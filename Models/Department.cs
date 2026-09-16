namespace ProductionMeeting.Models;

public class Department : AuditableEntity
{
    public int DepartmentId { get; set; }
    public string Name { get; set; } = null!;
}
