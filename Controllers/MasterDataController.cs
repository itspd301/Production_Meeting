using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProductionMeeting.Helpers;
using ProductionMeeting.Models;
using ProductionMeeting.Services;

namespace ProductionMeeting.Controllers;

[Authorize(Policy = PolicyNames.RequireAdmin)]
public class MasterDataController : Controller
{
    private readonly IMasterDataService _masterDataService;
    private readonly UserManager<ApplicationUser> _userManager;

    public MasterDataController(IMasterDataService masterDataService, UserManager<ApplicationUser> userManager)
    {
        _masterDataService = masterDataService;
        _userManager = userManager;
    }

    private string CurrentUserId => _userManager.GetUserId(User)!;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await _masterDataService.GetIndexAsync(cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePlant(int? id, string code, string name, CancellationToken cancellationToken)
    {
        var result = await _masterDataService.SavePlantAsync(id, code, name, CurrentUserId, cancellationToken);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLine(int? id, int plantId, string code, string name, CancellationToken cancellationToken)
    {
        var result = await _masterDataService.SaveLineAsync(id, plantId, code, name, CurrentUserId, cancellationToken);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveShift(int? id, string name, string? startTime, string? endTime, CancellationToken cancellationToken)
    {
        TimeSpan.TryParse(startTime, out var start);
        TimeSpan.TryParse(endTime, out var end);
        var result = await _masterDataService.SaveShiftAsync(id, name,
            string.IsNullOrWhiteSpace(startTime) ? null : start,
            string.IsNullOrWhiteSpace(endTime) ? null : end,
            CurrentUserId, cancellationToken);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDepartment(int? id, string name, CancellationToken cancellationToken)
    {
        var result = await _masterDataService.SaveDepartmentAsync(id, name, CurrentUserId, cancellationToken);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveModel(int? id, string name, CancellationToken cancellationToken)
    {
        var result = await _masterDataService.SaveModelAsync(id, name, CurrentUserId, cancellationToken);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveUnit(int? id, string name, CancellationToken cancellationToken)
    {
        var result = await _masterDataService.SaveUnitAsync(id, name, CurrentUserId, cancellationToken);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveIndicator(int? id, string code, string name, CancellationToken cancellationToken)
    {
        var result = await _masterDataService.SaveIndicatorAsync(id, code, name, CurrentUserId, cancellationToken);
        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(MasterDataType type, int id, bool isActive, CancellationToken cancellationToken)
    {
        var result = await _masterDataService.SetActiveAsync(type, id, isActive, CurrentUserId, cancellationToken);
        return Json(result);
    }
}
