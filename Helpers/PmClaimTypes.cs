namespace ProductionMeeting.Helpers;

public static class PmClaimTypes
{
    // Marks whether the incoming Windows identity has been matched to an app user record.
    // Value "false" means authenticated by Windows but not yet registered in the app.
    public const string Provisioned = "pm_provisioned";
    public const string FullName = "pm_full_name";
}
