using Microsoft.EntityFrameworkCore;
using ProductionMeeting.Data;
using ProductionMeeting.Models;
using ProductionMeeting.ViewModels;

namespace ProductionMeeting.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _db;

    public DashboardService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        var vm = new DashboardViewModel
        {
            TotalPlants = await _db.Plants.CountAsync(p => p.IsActive, cancellationToken),
            TotalLines = await _db.ProductionLines.CountAsync(l => l.IsActive, cancellationToken),
            TotalActiveKpis = await _db.KpiMasters.CountAsync(k => k.IsActive, cancellationToken),
            TotalSessions = await _db.MeetingSessions.CountAsync(cancellationToken),
            TodaySessionsCount = await _db.MeetingSessions.CountAsync(s => s.MeetingDate == today, cancellationToken)
        };

        var activeLineCount = vm.TotalLines;
        vm.PendingTodayCount = Math.Max(0, activeLineCount - vm.TodaySessionsCount);

        vm.RecentSessions = await _db.MeetingSessions
            .Include(s => s.Plant)
            .Include(s => s.Line)
            .Include(s => s.ConductedBy)
            .OrderByDescending(s => s.MeetingDate)
            .Take(8)
            .Select(s => new RecentSessionRow
            {
                SessionId = s.SessionId,
                PlantName = s.Plant.Name,
                LineName = s.Line.Name,
                MeetingDate = s.MeetingDate,
                Status = s.Status == MeetingSessionStatus.Completed ? "Completed" : "Draft",
                ConductedByName = s.ConductedBy.FullName
            })
            .ToListAsync(cancellationToken);

        return vm;
    }
}
