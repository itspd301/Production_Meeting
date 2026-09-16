namespace ProductionMeeting.ViewModels.ProductionMeeting;

public class MeetingIndexViewModel
{
    // Filters
    public int? PlantId { get; set; }
    public int? LineId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    // Dropdown sources
    public List<(int Id, string Name)> Plants { get; set; } = new();
    public List<(int Id, string Name)> Lines { get; set; } = new();
    public List<(int Id, string Name)> Shifts { get; set; } = new();

    // Quick-start new entry defaults
    public DateTime NewEntryDate { get; set; } = DateTime.Today;

    public List<MeetingListItemViewModel> Sessions { get; set; } = new();

    // Paging
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
