using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionMeeting.Helpers;
using ProductionMeeting.Services;

namespace ProductionMeeting.Controllers;

[Authorize(Policy = PolicyNames.RequireViewerOrAbove)]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var vm = await _dashboardService.GetDashboardAsync(cancellationToken);
        return View(vm);
    }
}
