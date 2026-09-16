namespace ProductionMeeting.Models;

// Field-level before/after audit trail — one row per changed field per save.
public class KpiTransactionAudit
{
    public int AuditId { get; set; }

    public int TransactionId { get; set; }
    public KpiTransaction Transaction { get; set; } = null!;

    public int KpiId { get; set; }
    public string FieldName { get; set; } = null!;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ActionType { get; set; } = null!; // INSERT / UPDATE

    public string ChangedByUserId { get; set; } = null!;
    public ApplicationUser ChangedBy { get; set; } = null!;
    public DateTime ChangedDate { get; set; } = DateTime.UtcNow;
}
