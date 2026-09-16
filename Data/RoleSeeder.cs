using Microsoft.AspNetCore.Identity;
using ProductionMeeting.Helpers;

namespace ProductionMeeting.Data;

// Ensures the fixed application roles exist. The first Admin user is provisioned
// automatically on first sign-in (see WindowsUserClaimsTransformation), not seeded here,
// since with Windows Authentication there's no local account to create in advance.
public static class RoleSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}
