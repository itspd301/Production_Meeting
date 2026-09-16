namespace ProductionMeeting.ViewModels.MasterData;

public class MasterRow
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }

    // Line only
    public int? PlantId { get; set; }
    public string? PlantName { get; set; }

    // Shift only
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
}

public class MasterDataIndexViewModel
{
    public List<MasterRow> Plants { get; set; } = new();
    public List<MasterRow> Lines { get; set; } = new();
    public List<MasterRow> Shifts { get; set; } = new();
    public List<MasterRow> Departments { get; set; } = new();
    public List<MasterRow> Models { get; set; } = new();
    public List<MasterRow> Units { get; set; } = new();
    public List<MasterRow> Indicators { get; set; } = new();

    // For the Line tab's Plant dropdown.
    public List<(int Id, string Name)> PlantOptions { get; set; } = new();
}
