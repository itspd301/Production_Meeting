using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionMeeting.Data;
using ProductionMeeting.Helpers;
using ProductionMeeting.Services;
using ProductionMeeting.ViewModels.UserManagement;

namespace ProductionMeeting.Controllers;

[Authorize(Policy = PolicyNames.RequireAdmin)]
public class UserManagementController : Controller
{
    private readonly IUserManagementService _userManagementService;
    private readonly ApplicationDbContext _db;

    public UserManagementController(IUserManagementService userManagementService, ApplicationDbContext db)
    {
        _userManagementService = userManagementService;
        _db = db;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var users = await _userManagementService.GetUsersAsync(cancellationToken);
        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var vm = await _userManagementService.GetNewUserFormAsync(cancellationToken);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            form.AvailableRoles = Roles.All.ToList();
            form.AvailablePlants = await _db.Plants.Where(p => p.IsActive).OrderBy(p => p.Name)
                .Select(p => new ValueTuple<int, string>(p.PlantId, p.Name)).ToListAsync(cancellationToken);
            return View(form);
        }

        var result = await _userManagementService.CreateUserAsync(form, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            form.AvailableRoles = Roles.All.ToList();
            form.AvailablePlants = await _db.Plants.Where(p => p.IsActive).OrderBy(p => p.Name)
                .Select(p => new ValueTuple<int, string>(p.PlantId, p.Name)).ToListAsync(cancellationToken);
            return View(form);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id, CancellationToken cancellationToken)
    {
        var vm = await _userManagementService.GetUserForEditAsync(id, cancellationToken);
        if (vm == null)
        {
            return NotFound();
        }
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserFormViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            form.AvailableRoles = Roles.All.ToList();
            form.AvailablePlants = await _db.Plants.Where(p => p.IsActive).OrderBy(p => p.Name)
                .Select(p => new ValueTuple<int, string>(p.PlantId, p.Name)).ToListAsync(cancellationToken);
            return View(form);
        }

        var result = await _userManagementService.UpdateUserAsync(form, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            form.AvailableRoles = Roles.All.ToList();
            form.AvailablePlants = await _db.Plants.Where(p => p.IsActive).OrderBy(p => p.Name)
                .Select(p => new ValueTuple<int, string>(p.PlantId, p.Name)).ToListAsync(cancellationToken);
            return View(form);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(string id, bool isActive, CancellationToken cancellationToken)
    {
        var result = await _userManagementService.SetActiveAsync(id, isActive, cancellationToken);
        return Ok(new { success = result.Success, message = result.Message });
    }

    [HttpGet]
    public async Task<IActionResult> GetLinesForPlant(int plantId, CancellationToken cancellationToken)
    {
        var lines = await _db.ProductionLines
            .Where(l => l.IsActive && l.PlantId == plantId)
            .OrderBy(l => l.Name)
            .Select(l => new { l.LineId, l.Name })
            .ToListAsync(cancellationToken);

        return Json(lines);
    }
}
