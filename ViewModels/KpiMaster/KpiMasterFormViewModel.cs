using System.ComponentModel.DataAnnotations;

namespace ProductionMeeting.ViewModels.KpiMaster;

public class KpiMasterFormViewModel
{
    public int? KpiId { get; set; }

    [Required(ErrorMessage = "Plant is required")]
    [Display(Name = "Plant")]
    public int PlantId { get; set; }

    [Required(ErrorMessage = "Shop is required")]
    [Display(Name = "Shop")]
    public int LineId { get; set; }

    [Required(ErrorMessage = "Indicator is required")]
    [Display(Name = "Indicator")]
    public int IndicatorId { get; set; }

    [Required(ErrorMessage = "Unit is required")]
    [Display(Name = "Unit")]
    public int UnitId { get; set; }

    [Required(ErrorMessage = "Description is required")]
    [StringLength(500)]
    public string Description { get; set; } = null!;

    [Required(ErrorMessage = "Display order is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Display order must be greater than 0")]
    [Display(Name = "Display Order")]
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    [Display(Name = "Lower value is better")]
    public bool LowerIsBetter { get; set; }

    [Display(Name = "F26")]
    public decimal? F26Value { get; set; }

    [Display(Name = "F27 / L4 Target")]
    public decimal? F27Value { get; set; }

    [Display(Name = "Fetch value via stored procedure")]
    public bool IsSourcedFromStoredProcedure { get; set; }

    [Display(Name = "Stored procedure name")]
    [StringLength(200)]
    public string? StoredProcedureName { get; set; }

    public List<(int Id, string Name)> Plants { get; set; } = new();
    public List<(int Id, string Name)> Lines { get; set; } = new();
    public List<(int Id, string Code, string Name)> Indicators { get; set; } = new();
    public List<(int Id, string Name)> Units { get; set; } = new();
}
