namespace ProductionMeeting.Helpers;

public static class KpiSources
{
    public const string Manual = "Manual";
    public const string Calculated = "Calculated";
    public const string StoredProcedure = "SP";

    public static readonly string[] All = { Manual, Calculated, StoredProcedure };
}
