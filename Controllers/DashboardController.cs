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
    private readonly IExecutiveDashboardService _executiveDashboardService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(
        IDashboardService dashboardService,
        IExecutiveDashboardService executiveDashboardService,
        UserManager<ApplicationUser> userManager)
    {
        _dashboardService = dashboardService;
        _executiveDashboardService = executiveDashboardService;
        _userManager = userManager;
    }

    private string CurrentUserId => _userManager.GetUserId(User)!;
    private bool IsAdmin => User.IsInRole(Roles.Admin);
    private bool CanEditRemarks => User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Manager) || User.IsInRole(Roles.ProductionUser);

    public async Task<IActionResult> Index(DashboardViewModel filters, CancellationToken cancellationToken)
    {
        var vm = await _dashboardService.GetDashboardAsync(filters, CurrentUserId, IsAdmin, cancellationToken);
        return View(vm);
    }

    public async Task<IActionResult> Executive(ExecutiveDashboardViewModel filters, CancellationToken cancellationToken)
    {
        var vm = await _executiveDashboardService.GetDashboardAsync(filters, CurrentUserId, IsAdmin, cancellationToken);
        vm.CanEditRemarks = CanEditRemarks;
        return View(vm);
    }

    [Authorize(Policy = PolicyNames.RequireProductionUserOrAbove)]
    [HttpPost]
    public async Task<IActionResult> SaveRemarks(int sessionId, string? remarks, CancellationToken cancellationToken)
    {
        var result = await _executiveDashboardService.SaveRemarksAsync(sessionId, remarks, CurrentUserId, cancellationToken);
        return Ok(new { success = result.Success, message = result.Message });
    }

    [HttpGet]
    public async Task<IActionResult> GetKpiTrend(int kpiId, int plantId, int lineId, string week, CancellationToken cancellationToken)
    {
        var weekStart = PmDates.FromWeekInputValue(week) ?? DateTime.Today;
        var result = await _executiveDashboardService.GetKpiTrendAsync(kpiId, plantId, lineId, weekStart, cancellationToken);

        if (result == null)
        {
            return NotFound();
        }

        return Json(result);
    }
}
