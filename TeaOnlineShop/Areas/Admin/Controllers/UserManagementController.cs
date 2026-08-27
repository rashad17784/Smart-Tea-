using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeaOnlineShop.Authorization;
using TeaOnlineShop.Identity;
using TeaOnlineShop.Models.ViewModels;

namespace TeaOnlineShop.Areas.Admin.Controllers;

[Authorize(Policy = AppPermissions.UsersManage)]
public sealed class UserManagementController : AdminBaseController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationIdentityContext _identityContext;

    public UserManagementController(
        UserManager<ApplicationUser> userManager,
        ApplicationIdentityContext identityContext)
    {
        _userManager = userManager;
        _identityContext = identityContext;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.OrderBy(x => x.FullName).ToListAsync();
        var managedUsers = new List<ManagedUserViewModel>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var currentRole = roles.FirstOrDefault() ?? string.Empty;
            managedUsers.Add(new ManagedUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Roles = string.Join(", ", roles),
                CurrentRole = currentRole,
                IsActive = user.IsActive,
                TwoFactorEnabled = user.TwoFactorEnabled,
                MfaRequired = roles.Any(AppRoles.RequiresMfa),
                CreatedAtUtc = user.CreatedAtUtc,
                LastLoginAtUtc = user.LastLoginAtUtc
            });
        }

        return View(new UserManagementIndexViewModel
        {
            Users = managedUsers,
            AvailableInternalRoles = AppRoles.InternalRoles
        });
    }

    [HttpGet]
    public IActionResult CreateStaff(string? role = null) => View(new CreateStaffUserViewModel
    {
        Role = AppRoles.IsInternal(role) ? role! : AppRoles.WarehouseStaff
    });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStaff(CreateStaffUserViewModel model)
    {
        if (!AppRoles.IsInternal(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "Select an approved internal staff role.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim().ToLowerInvariant();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = model.FullName.Trim(),
            IsActive = true,
            RequiresPasswordChange = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, model.TemporaryPassword);
        if (!createResult.Succeeded)
        {
            AddIdentityErrors(createResult);
            return View(model);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            AddIdentityErrors(roleResult);
            return View(model);
        }

        await RecordSecurityAuditAsync(
            "StaffAccountCreated",
            user,
            $"Internal account created with role {model.Role}; password change and MFA enrollment required.");

        TempData["SuccessMessage"] =
            $"{model.Role} account created. The user must change the temporary password and enroll MFA at first login.";
        return RedirectToAction(nameof(Index));
    }

    // Backwards-compatible route for existing bookmarks.
    [HttpGet]
    public IActionResult CreateWarehouseStaff() =>
        RedirectToAction(nameof(CreateStaff), new { role = AppRoles.WarehouseStaff });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CreateWarehouseStaff(CreateWarehouseUserViewModel model) =>
        CreateStaff(new CreateStaffUserViewModel
        {
            FullName = model.FullName,
            Email = model.Email,
            TemporaryPassword = model.TemporaryPassword,
            Role = AppRoles.WarehouseStaff
        });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(ChangeUserRoleViewModel model)
    {
        if (!ModelState.IsValid || !AppRoles.IsInternal(model.Role))
        {
            TempData["ErrorMessage"] = "Select a valid internal staff role.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(model.UserId.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var actorId = CurrentActorId();
        if (user.Id == actorId)
        {
            TempData["ErrorMessage"] = "You cannot change your own role. A different Administrator must perform this action.";
            return RedirectToAction(nameof(Index));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Contains(model.Role, StringComparer.OrdinalIgnoreCase) && currentRoles.Count == 1)
        {
            TempData["SuccessMessage"] = "The account already has the selected role.";
            return RedirectToAction(nameof(Index));
        }

        if (currentRoles.Contains(AppRoles.Administrator, StringComparer.OrdinalIgnoreCase) &&
            !string.Equals(model.Role, AppRoles.Administrator, StringComparison.OrdinalIgnoreCase) &&
            !await HasAnotherActiveAdministratorAsync(user.Id))
        {
            TempData["ErrorMessage"] = "The final active Administrator cannot be demoted.";
            return RedirectToAction(nameof(Index));
        }

        await using var transaction = await _identityContext.Database.BeginTransactionAsync();
        var addResult = currentRoles.Contains(model.Role, StringComparer.OrdinalIgnoreCase)
            ? IdentityResult.Success
            : await _userManager.AddToRoleAsync(user, model.Role);
        if (!addResult.Succeeded)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = string.Join(" ", addResult.Errors.Select(x => x.Description));
            return RedirectToAction(nameof(Index));
        }

        var rolesToRemove = currentRoles
            .Where(x => !string.Equals(x, model.Role, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (rolesToRemove.Length > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = string.Join(" ", removeResult.Errors.Select(x => x.Description));
                return RedirectToAction(nameof(Index));
            }
        }

        var stampResult = await _userManager.UpdateSecurityStampAsync(user);
        if (!stampResult.Succeeded)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "The role changed but session revocation failed; the transaction was cancelled.";
            return RedirectToAction(nameof(Index));
        }

        await RecordSecurityAuditAsync(
            "RoleChanged",
            user,
            $"Role changed from [{string.Join(", ", currentRoles)}] to [{model.Role}]. Existing sessions revoked.");
        await transaction.CommitAsync();

        TempData["SuccessMessage"] =
            $"{user.Email} is now {model.Role}. Existing sessions were revoked and MFA is required at next login.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(int id, bool active)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        if (user.Id == CurrentActorId() && !active)
        {
            TempData["ErrorMessage"] = "You cannot deactivate your own account.";
            return RedirectToAction(nameof(Index));
        }

        if (!active && await _userManager.IsInRoleAsync(user, AppRoles.Administrator) &&
            !await HasAnotherActiveAdministratorAsync(user.Id))
        {
            TempData["ErrorMessage"] = "The final active Administrator cannot be deactivated.";
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = active;
        var updateResult = await _userManager.UpdateAsync(user);
        var stampResult = updateResult.Succeeded
            ? await _userManager.UpdateSecurityStampAsync(user)
            : updateResult;
        if (!updateResult.Succeeded || !stampResult.Succeeded)
        {
            TempData["ErrorMessage"] = "The account status could not be changed safely.";
            return RedirectToAction(nameof(Index));
        }

        await RecordSecurityAuditAsync(
            active ? "AccountActivated" : "AccountDeactivated",
            user,
            active ? "Account activated; existing sessions revoked." : "Account deactivated; existing sessions revoked.");
        TempData["SuccessMessage"] = active
            ? "Account activated. The user must authenticate again."
            : "Account deactivated and all sessions revoked.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeSessions(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var result = await _userManager.UpdateSecurityStampAsync(user);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = "Sessions could not be revoked.";
            return RedirectToAction(nameof(Index));
        }

        await RecordSecurityAuditAsync("SessionsRevoked", user, "All existing authentication sessions revoked.");
        TempData["SuccessMessage"] = "All existing sessions for the account have been revoked.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> LoginHistory()
    {
        return View(await _identityContext.LoginHistories
            .AsNoTracking()
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(500)
            .ToListAsync());
    }

    public async Task<IActionResult> SecurityAudit()
    {
        return View(await _identityContext.SecurityAuditEvents
            .AsNoTracking()
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(500)
            .ToListAsync());
    }

    private async Task<bool> HasAnotherActiveAdministratorAsync(int excludedUserId)
    {
        var administrators = await _userManager.GetUsersInRoleAsync(AppRoles.Administrator);
        return administrators.Any(x => x.Id != excludedUserId && x.IsActive);
    }

    private int CurrentActorId() =>
        int.TryParse(_userManager.GetUserId(User), out var id) ? id : 0;

    private async Task RecordSecurityAuditAsync(string action, ApplicationUser target, string detail)
    {
        _identityContext.SecurityAuditEvents.Add(new SecurityAuditEvent
        {
            Action = action,
            ActorUserId = CurrentActorId(),
            ActorEmail = User.Identity?.Name ?? "unknown",
            TargetUserId = target.Id,
            TargetEmail = target.Email ?? target.UserName ?? "unknown",
            Detail = detail,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            OccurredAtUtc = DateTime.UtcNow
        });
        await _identityContext.SaveChangesAsync();
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
