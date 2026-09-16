using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProductionMeeting.Helpers;
using ProductionMeeting.Models;
using ProductionMeeting.Services;
using ProductionMeeting.ViewModels.ProductionMeeting;

namespace ProductionMeeting.Controllers;

[Authorize(Policy = PolicyNames.RequireViewerOrAbove)]
public class ProductionMeetingController : Controller
{
    private readonly IProductionMeetingService _meetingService;
    private readonly IUserAccessService _accessService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProductionMeetingController(
        IProductionMeetingService meetingService,
        IUserAccessService accessService,
        UserManager<ApplicationUser> userManager)
    {
        _meetingService = meetingService;
        _accessService = accessService;
        _userManager = userManager;
    }

    private string CurrentUserId => _userManager.GetUserId(User)!;
    private bool IsAdmin => User.IsInRole(Roles.Admin);

    public async Task<IActionResult> Index(MeetingIndexViewModel filters, CancellationToken cancellationToken)
    {
        var vm = await _meetingService.GetIndexAsync(filters, CurrentUserId, IsAdmin, cancellationToken);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> GetLines(int plantId, CancellationToken cancellationToken)
    {
        var lines = await _accessService.GetAccessibleLinesAsync(CurrentUserId, IsAdmin, plantId, cancellationToken);
        return Json(lines.Select(l => new { l.LineId, l.Name }));
    }

    [Authorize(Policy = PolicyNames.RequireProductionUserOrAbove)]
    [HttpGet]
    public async Task<IActionResult> Entry(int plantId, int lineId, int? shiftId, DateTime? meetingDate, CancellationToken cancellationToken)
    {
        var date = meetingDate ?? DateTime.Today;
        var vm = await _meetingService.GetOrCreateEntryAsync(plantId, lineId, shiftId, date, CurrentUserId, IsAdmin, cancellationToken);

        if (vm == null)
        {
            return Forbid();
        }

        return View(vm);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var vm = await _meetingService.GetEntryBySessionIdAsync(id, CurrentUserId, IsAdmin, readOnly: true, cancellationToken);
        if (vm == null)
        {
            return NotFound();
        }

        ViewBag.AuditHistory = await _meetingService.GetSessionAuditHistoryAsync(id, cancellationToken);

        return View(vm);
    }

    [Authorize(Policy = PolicyNames.RequireProductionUserOrAbove)]
    [HttpPost]
    public async Task<IActionResult> SaveEntry([FromBody] SaveEntryRequest request, CancellationToken cancellationToken)
    {
        if (request == null || request.Rows == null)
        {
            return BadRequest(new { success = false, message = "Invalid request." });
        }

        var user = await _userManager.GetUserAsync(User);
        var result = await _meetingService.SaveEntryAsync(request, CurrentUserId, user?.FullName ?? "Unknown", cancellationToken);

        return Ok(new { success = result.Success, message = result.Message });
    }
}
