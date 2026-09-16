using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProductionMeeting.Data;
using ProductionMeeting.Helpers;
using ProductionMeeting.Models;
using ProductionMeeting.ViewModels.UserManagement;

namespace ProductionMeeting.Services;

public class UserManagementService : IUserManagementService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserManagementService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<List<UserListItemViewModel>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _db.Users.OrderBy(u => u.FullName).ToListAsync(cancellationToken);

        var userRoles = await (from ur in _db.UserRoles
                                join r in _db.Roles on ur.RoleId equals r.Id
                                select new { ur.UserId, RoleName = r.Name }).ToListAsync(cancellationToken);

        var access = await _db.UserPlantLineAccesses
            .Include(a => a.Plant)
            .Include(a => a.Line)
            .ToListAsync(cancellationToken);

        return users.Select(u =>
        {
            var role = userRoles.FirstOrDefault(r => r.UserId == u.Id)?.RoleName ?? "(none)";
            string accessSummary;

            if (role == Roles.Admin)
            {
                accessSummary = "All Plants";
            }
            else
            {
                var userAccess = access.Where(a => a.UserId == u.Id).ToList();
                if (userAccess.Count == 0)
                {
                    accessSummary = "None";
                }
                else
                {
                    accessSummary = string.Join(", ", userAccess
                        .GroupBy(a => a.Plant.Name)
                        .Select(g => g.Any(x => x.LineId == null)
                            ? $"{g.Key} (All Lines)"
                            : $"{g.Key} ({string.Join("/", g.Select(x => x.Line!.Name))})"));
                }
            }

            return new UserListItemViewModel
            {
                Id = u.Id,
                UserName = u.UserName!,
                FullName = u.FullName,
                Role = role,
                IsActive = u.IsActive,
                AccessSummary = accessSummary
            };
        }).ToList();
    }

    public async Task<UserFormViewModel> GetNewUserFormAsync(CancellationToken cancellationToken = default)
    {
        return new UserFormViewModel
        {
            IsActive = true,
            AvailableRoles = Roles.All.ToList(),
            AvailablePlants = await GetPlantOptionsAsync(cancellationToken)
        };
    }

    public async Task<UserFormViewModel?> GetUserForEditAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);

        var access = await _db.UserPlantLineAccesses
            .Include(a => a.Plant)
            .Include(a => a.Line)
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken);

        return new UserFormViewModel
        {
            Id = user.Id,
            UserName = user.UserName!,
            FullName = user.FullName,
            Role = roles.FirstOrDefault() ?? Roles.Viewer,
            IsActive = user.IsActive,
            PlantAccess = access.Select(a => new PlantAccessRow
            {
                PlantId = a.PlantId,
                PlantName = a.Plant.Name,
                LineId = a.LineId,
                LineName = a.Line?.Name
            }).ToList(),
            AvailableRoles = Roles.All.ToList(),
            AvailablePlants = await GetPlantOptionsAsync(cancellationToken)
        };
    }

    public async Task<ServiceResult> CreateUserAsync(UserFormViewModel form, CancellationToken cancellationToken = default)
    {
        var existing = await _userManager.FindByNameAsync(form.UserName);
        if (existing != null)
        {
            return new ServiceResult { Success = false, Message = "A user with this Windows account already exists." };
        }

        var user = new ApplicationUser
        {
            UserName = form.UserName.Trim(),
            FullName = form.FullName.Trim(),
            IsActive = form.IsActive,
            EmailConfirmed = true
        };

        // No password: authentication is Windows Authentication, not a local credential.
        var createResult = await _userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            return new ServiceResult { Success = false, Message = string.Join("; ", createResult.Errors.Select(e => e.Description)) };
        }

        await _userManager.AddToRoleAsync(user, form.Role);
        await SavePlantAccessAsync(user.Id, form.Role, form.PlantAccess, cancellationToken);

        return new ServiceResult { Success = true, Message = "User created successfully." };
    }

    public async Task<ServiceResult> UpdateUserAsync(UserFormViewModel form, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(form.Id))
        {
            return new ServiceResult { Success = false, Message = "Invalid user." };
        }

        var user = await _userManager.FindByIdAsync(form.Id);
        if (user == null)
        {
            return new ServiceResult { Success = false, Message = "User not found." };
        }

        user.FullName = form.FullName.Trim();
        user.IsActive = form.IsActive;
        await _userManager.UpdateAsync(user);

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
        }
        await _userManager.AddToRoleAsync(user, form.Role);

        await SavePlantAccessAsync(user.Id, form.Role, form.PlantAccess, cancellationToken);

        return new ServiceResult { Success = true, Message = "User updated successfully." };
    }

    public async Task<ServiceResult> SetActiveAsync(string userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return new ServiceResult { Success = false, Message = "User not found." };
        }

        user.IsActive = isActive;
        await _userManager.UpdateAsync(user);

        return new ServiceResult { Success = true, Message = isActive ? "User activated." : "User deactivated." };
    }

    private async Task SavePlantAccessAsync(string userId, string role, List<PlantAccessRow> rows, CancellationToken cancellationToken)
    {
        var existing = _db.UserPlantLineAccesses.Where(a => a.UserId == userId);
        _db.UserPlantLineAccesses.RemoveRange(existing);

        // Admins bypass plant/line scoping entirely (see IUserAccessService), so there is
        // nothing meaningful to store for them even if the form posted rows.
        if (role != Roles.Admin)
        {
            foreach (var row in rows)
            {
                _db.UserPlantLineAccesses.Add(new UserPlantLineAccess
                {
                    UserId = userId,
                    PlantId = row.PlantId,
                    LineId = row.LineId
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<(int Id, string Name)>> GetPlantOptionsAsync(CancellationToken cancellationToken)
    {
        return await _db.Plants.Where(p => p.IsActive).OrderBy(p => p.Name)
            .Select(p => new ValueTuple<int, string>(p.PlantId, p.Name))
            .ToListAsync(cancellationToken);
    }
}
