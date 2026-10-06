using System.ComponentModel.DataAnnotations.Schema;

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

    // True for KPIs where a smaller number is the good outcome (defects, cost, incidents),
    // false where bigger is better (schedule adherence, straight pass ratio). Drives the
    // Green/Amber/Red status shown on the dashboard.
    public bool LowerIsBetter { get; set; }

    // Fixed once a year by an admin here, not re-typed weekly. The Entry grid pre-fills
    // each week's F26/F27 cells from these (labels shift forward every April - see
    // PmDates.FiscalTargetLabels) but still lets someone override a single week's value.
    [Column(TypeName = "decimal(18,2)")]
    public decimal? F26Value { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? F27Value { get; set; }

    // When set, the Entry grid auto-fills this KPI's Week value by calling the named
    // stored procedure instead of waiting for manual entry (still editable afterward -
    // see ProductionMeetingService.TryGetScalarFromProcedureAsync for the calling
    // convention: EXEC <proc> @PlantId, @LineId, @WeekStart).
    public bool IsSourcedFromStoredProcedure { get; set; }
    public string? StoredProcedureName { get; set; }
}
