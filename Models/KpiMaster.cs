namespace ProductionMeeting.Models;

// Each KPI belongs to one Plant + Line, so different lines can have their own scorecards.
public class KpiMaster : AuditableEntity
{
    public int KpiId { get; set; }

    public int PlantId { get; set; }
    public Plant Plant { get; set; } = null!;

    public int LineId { get; set; }
    public ProductionLine Line { get; set; } = null!;

    public int IndicatorId { get; set; }
    public Indicator Indicator { get; set; } = null!;

    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    public string Description { get; set; } = null!;
    public int DisplayOrder { get; set; }
}
