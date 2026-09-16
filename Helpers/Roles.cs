namespace ProductionMeeting.Helpers;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string ProductionUser = "ProductionUser";
    public const string Viewer = "Viewer";

    public static readonly string[] All = { Admin, Manager, ProductionUser, Viewer };
}
