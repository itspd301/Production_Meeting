namespace ProductionMeeting.Models;

public enum MeetingSessionStatus
{
    Draft = 0,
    Completed = 1
}

// Header record binding a KPI entry grid to a specific plant + line (+ optional shift) + date.
public class MeetingSession : AuditableEntity
{
    public int SessionId { get; set; }

    public int PlantId { get; set; }
    public Plant Plant { get; set; } = null!;

    public int LineId { get; set; }
    public ProductionLine Line { get; set; } = null!;

    public int? ShiftId { get; set; }
    public Shift? Shift { get; set; }

    public DateTime MeetingDate { get; set; }

    public string ConductedByUserId { get; set; } = null!;
    public ApplicationUser ConductedBy { get; set; } = null!;

    public string? Remarks { get; set; }
    public MeetingSessionStatus Status { get; set; } = MeetingSessionStatus.Draft;

    public ICollection<KpiTransaction> Transactions { get; set; } = new List<KpiTransaction>();
}
