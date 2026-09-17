using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProductionMeeting.Models;

namespace ProductionMeeting.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Plant> Plants => Set<Plant>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<ProductionLine> ProductionLines => Set<ProductionLine>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<ProductModel> ProductModels => Set<ProductModel>();
    public DbSet<Indicator> Indicators => Set<Indicator>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<KpiMaster> KpiMasters => Set<KpiMaster>();
    public DbSet<MeetingSession> MeetingSessions => Set<MeetingSession>();
    public DbSet<KpiTransaction> KpiTransactions => Set<KpiTransaction>();
    public DbSet<KpiTransactionAudit> KpiTransactionAudits => Set<KpiTransactionAudit>();
    public DbSet<UserPlantLineAccess> UserPlantLineAccesses => Set<UserPlantLineAccess>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Explicit keys for entities whose PK property name doesn't match EF Core's
        // "{TypeName}Id" convention (e.g. KpiMaster.KpiId, ProductionLine.LineId).
        builder.Entity<ProductionLine>().HasKey(l => l.LineId);
        builder.Entity<ProductModel>().HasKey(m => m.ModelId);
        builder.Entity<KpiMaster>().HasKey(k => k.KpiId);
        builder.Entity<MeetingSession>().HasKey(s => s.SessionId);
        builder.Entity<KpiTransaction>().HasKey(t => t.TransactionId);
        builder.Entity<KpiTransactionAudit>().HasKey(a => a.AuditId);
        builder.Entity<UserPlantLineAccess>().HasKey(a => a.AccessId);

        builder.Entity<ProductionLine>()
            .HasOne(l => l.Plant).WithMany(p => p.ProductionLines)
            .HasForeignKey(l => l.PlantId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ProductionLine>()
            .HasIndex(l => new { l.PlantId, l.Code }).IsUnique();

        builder.Entity<KpiMaster>()
            .HasOne(k => k.Plant).WithMany().HasForeignKey(k => k.PlantId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<KpiMaster>()
            .HasOne(k => k.Line).WithMany().HasForeignKey(k => k.LineId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<KpiMaster>()
            .HasOne(k => k.Indicator).WithMany().HasForeignKey(k => k.IndicatorId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<KpiMaster>()
            .HasOne(k => k.Unit).WithMany().HasForeignKey(k => k.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<KpiMaster>()
            .HasIndex(k => new { k.PlantId, k.LineId, k.IndicatorId, k.Description }).IsUnique();

        builder.Entity<MeetingSession>()
            .HasOne(s => s.Plant).WithMany().HasForeignKey(s => s.PlantId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MeetingSession>()
            .HasOne(s => s.Line).WithMany().HasForeignKey(s => s.LineId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MeetingSession>()
            .HasOne(s => s.Shift).WithMany().HasForeignKey(s => s.ShiftId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MeetingSession>()
            .HasOne(s => s.ConductedBy).WithMany().HasForeignKey(s => s.ConductedByUserId).OnDelete(DeleteBehavior.Restrict);
        // Shift is no longer collected in the meeting flow, so uniqueness is one session per
        // plant/shop/week (MeetingDate is always normalized to that week's Monday).
        builder.Entity<MeetingSession>()
            .HasIndex(s => new { s.PlantId, s.LineId, s.MeetingDate }).IsUnique();

        builder.Entity<KpiTransaction>()
            .HasOne(t => t.Session).WithMany(s => s.Transactions)
            .HasForeignKey(t => t.SessionId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<KpiTransaction>()
            .HasOne(t => t.Kpi).WithMany().HasForeignKey(t => t.KpiId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<KpiTransaction>()
            .HasOne(t => t.Model).WithMany().HasForeignKey(t => t.ModelId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<KpiTransaction>()
            .HasIndex(t => new { t.SessionId, t.KpiId, t.ModelId }).IsUnique();

        builder.Entity<KpiTransactionAudit>()
            .HasOne(a => a.Transaction).WithMany().HasForeignKey(a => a.TransactionId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<KpiTransactionAudit>()
            .HasOne(a => a.ChangedBy).WithMany().HasForeignKey(a => a.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserPlantLineAccess>()
            .HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<UserPlantLineAccess>()
            .HasOne(a => a.Plant).WithMany().HasForeignKey(a => a.PlantId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<UserPlantLineAccess>()
            .HasOne(a => a.Line).WithMany().HasForeignKey(a => a.LineId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<UserPlantLineAccess>()
            .HasIndex(a => new { a.UserId, a.PlantId, a.LineId }).IsUnique();

        SeedData.Seed(builder);
    }
}
