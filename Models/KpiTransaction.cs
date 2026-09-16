using System.ComponentModel.DataAnnotations.Schema;

namespace ProductionMeeting.Models;

public class KpiTransaction : AuditableEntity
{
    public int TransactionId { get; set; }

    public int SessionId { get; set; }
    public MeetingSession Session { get; set; } = null!;

    public int KpiId { get; set; }
    public KpiMaster Kpi { get; set; } = null!;

    public int? ModelId { get; set; }
    public ProductModel? Model { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? F26Value { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? F27Value { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? WeekValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MonthCumValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? YtdValue { get; set; }

    public string? Remarks { get; set; }
}
