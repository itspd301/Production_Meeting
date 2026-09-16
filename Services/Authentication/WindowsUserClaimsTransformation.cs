using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using ProductionMeeting.Helpers;
using ProductionMeeting.Models;

namespace ProductionMeeting.Services.Authentication;

// Windows Authentication (Negotiate) only proves *who* the OS says the caller is
// (DOMAIN\username) - it carries no app roles. This runs once per request to look up
// (or, for the very first user ever, auto-provision as Admin) the matching ApplicationUser
// and attach role/identity claims the rest of the app already expects.
public class WindowsUserClaimsTransformation : IClaimsTransformation
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<WindowsUserClaimsTransformation> _logger;

    public WindowsUserClaimsTransformation(UserManager<ApplicationUser> userManager, ILogger<WindowsUserClaimsTransformation> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not { IsAuthenticated: true } identity)
        {
            return principal;
        }

        if (principal.HasClaim(c => c.Type == PmClaimTypes.Provisioned))
        {
            // Already transformed earlier in this request pipeline.
            return principal;
        }

        var windowsUserName = identity.Name;
        if (string.IsNullOrWhiteSpace(windowsUserName))
        {
            return principal;
        }

        var user = await _userManager.FindByNameAsync(windowsUserName);

        if (user == null)
        {
            var isFirstEverUser = !_userManager.Users.Any();
            if (!isFirstEverUser)
            {
                return WithClaim(principal, new Claim(PmClaimTypes.Provisioned, "false"));
            }

            // Bootstrap: the very first person to reach the app becomes Admin so there is
            // always at least one account that can provision everyone else via User Management.
            user = new ApplicationUser
            {
                UserName = windowsUserName,
                FullName = windowsUserName,
                EmailConfirmed = true,
                IsActive = true
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                _logger.LogError(
                    "Failed to auto-provision first Admin user for Windows account {WindowsUserName}: {Errors}",
                    windowsUserName, string.Join("; ", createResult.Errors.Select(e => e.Description)));
                return WithClaim(principal, new Claim(PmClaimTypes.Provisioned, "false"));
            }

            await _userManager.AddToRoleAsync(user, Roles.Admin);
            _logger.LogWarning("Auto-provisioned {WindowsUserName} as the first Admin user.", windowsUserName);
        }

        if (!user.IsActive)
        {
            return WithClaim(principal, new Claim(PmClaimTypes.Provisioned, "false"));
        }

        var roles = await _userManager.GetRolesAsync(user);

        var newIdentity = new ClaimsIdentity(identity.AuthenticationType, ClaimTypes.Name, ClaimTypes.Role);
        newIdentity.AddClaim(new Claim(ClaimTypes.Name, windowsUserName));
        newIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
        newIdentity.AddClaim(new Claim(PmClaimTypes.FullName, user.FullName));
        newIdentity.AddClaim(new Claim(PmClaimTypes.Provisioned, "true"));
        foreach (var role in roles)
        {
            newIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        return new ClaimsPrincipal(newIdentity);
    }

    private static ClaimsPrincipal WithClaim(ClaimsPrincipal principal, Claim claim)
    {
        ((ClaimsIdentity)principal.Identity!).AddClaim(claim);
        return principal;
    }
}
