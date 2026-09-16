using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProductionMeeting.Helpers;
using ProductionMeeting.Models;
using ProductionMeeting.Services;
using ProductionMeeting.ViewModels.KpiMaster;

namespace ProductionMeeting.Controllers;

[Authorize(Policy = PolicyNames.RequireAdmin)]
public class KpiMasterController : Controller
{
    private readonly IKpiMasterService _kpiMasterService;
    private readonly UserManager<ApplicationUser> _userManager;

    public KpiMasterController(IKpiMasterService kpiMasterService, UserManager<ApplicationUser> userManager)
    {
        _kpiMasterService = kpiMasterService;
        _userManager = userManager;
    }

    private string CurrentUserId => _userManager.GetUserId(User)!;

    public async Task<IActionResult> Index(KpiMasterIndexViewModel filters, CancellationToken cancellationToken)
    {
        var vm = await _kpiMasterService.GetIndexAsync(filters, cancellationToken);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> GetLinesForPlant(int plantId, CancellationToken cancellationToken)
    {
        var lines = await _kpiMasterService.GetLinesForPlantAsync(plantId, cancellationToken);
        return Json(lines.Select(l => new { lineId = l.Id, name = l.Name }));
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View(await _kpiMasterService.GetNewFormAsync(cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KpiMasterFormViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await RepopulateAsync(form, cancellationToken);
            return View(form);
        }

        var result = await _kpiMasterService.CreateAsync(form, CurrentUserId, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await RepopulateAsync(form, cancellationToken);
            return View(form);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var vm = await _kpiMasterService.GetForEditAsync(id, cancellationToken);
        if (vm == null)
        {
            return NotFound();
        }
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(KpiMasterFormViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await RepopulateAsync(form, cancellationToken);
            return View(form);
        }

        var result = await _kpiMasterService.UpdateAsync(form, CurrentUserId, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await RepopulateAsync(form, cancellationToken);
            return View(form);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(int id, bool isActive, CancellationToken cancellationToken)
    {
        var result = await _kpiMasterService.SetActiveAsync(id, isActive, CurrentUserId, cancellationToken);
        return Ok(new { success = result.Success, message = result.Message });
    }

    private async Task RepopulateAsync(KpiMasterFormViewModel form, CancellationToken cancellationToken)
    {
        var fresh = await _kpiMasterService.GetNewFormAsync(cancellationToken);
        form.Plants = fresh.Plants;
        form.Indicators = fresh.Indicators;
        form.Units = fresh.Units;
        form.Lines = form.PlantId > 0 ? await _kpiMasterService.GetLinesForPlantAsync(form.PlantId, cancellationToken) : new();
    }
}
