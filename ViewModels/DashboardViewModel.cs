namespace ProductionMeeting.ViewModels;

public class DashboardViewModel
{
    public int TotalPlants { get; set; }
    public int TotalLines { get; set; }
    public int TotalActiveKpis { get; set; }

    public int TotalSessions { get; set; }
    public int TodaySessionsCount { get; set; }
    public int PendingTodayCount { get; set; }

    public List<RecentSessionRow> RecentSessions { get; set; } = new();
}

public class RecentSessionRow
{
    public int SessionId { get; set; }
    public string PlantName { get; set; } = null!;
    public string LineName { get; set; } = null!;
    public DateTime MeetingDate { get; set; }
    public string Status { get; set; } = null!;
    public string ConductedByName { get; set; } = null!;
}
