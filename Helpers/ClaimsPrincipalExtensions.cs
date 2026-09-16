using System.Security.Claims;

namespace ProductionMeeting.Helpers;

public static class ClaimsPrincipalExtensions
{
    public static string GetDisplayName(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(PmClaimTypes.FullName)?.Value
            ?? principal.Identity?.Name
            ?? "Unknown";
    }

    public static bool IsProvisioned(this ClaimsPrincipal principal)
    {
        return principal.HasClaim(c => c.Type == PmClaimTypes.Provisioned && c.Value == "true");
    }
}
