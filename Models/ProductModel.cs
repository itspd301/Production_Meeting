namespace ProductionMeeting.Models;

// Vehicle model (e.g. XUV300). Named "ProductModel" to avoid clashing with the
// MVC "Models" concept / ASP.NET Core Identity's own naming.
public class ProductModel : AuditableEntity
{
    public int ModelId { get; set; }
    public string Name { get; set; } = null!;
}
