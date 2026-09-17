using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProductionMeeting.Helpers;
using ProductionMeeting.Models;
using ProductionMeeting.Services;
using ProductionMeeting.ViewModels;

namespace ProductionMeeting.Controllers;

[Authorize(Policy = PolicyNames.RequireViewerOrAbove)]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(IDashboardService dashboardService, UserManager<ApplicationUser> userManager)
    {
        _dashboardService = dashboardService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(DashboardViewModel filters, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User)!;
        var isAdmin = User.IsInRole(Roles.Admin);

        var vm = await _dashboardService.GetDashboardAsync(filters, userId, isAdmin, cancellationToken);
        return View(vm);
    }
}
