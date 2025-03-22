using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySanPhamApp.Models.Identity;
using System.Security.Claims;

namespace QuanLySanPhamApp.Controllers;

[Authorize(Roles = "Admin")]
public class UserManagementController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<UserManagementController> _logger;

    public UserManagementController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger<UserManagementController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.ToListAsync();
        return View(users);
    }

    public async Task<IActionResult> Details(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        ViewBag.Roles = await _userManager.GetRolesAsync(user);
        ViewBag.Claims = await _userManager.GetClaimsAsync(user);
        
        return View(user);
    }

    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var model = new EditUserViewModel
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DateOfBirth = user.DateOfBirth,
            EmailConfirmed = user.EmailConfirmed
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null)
        {
            return NotFound();
        }

        user.Email = model.Email;
        user.UserName = model.Email;
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.DateOfBirth = model.DateOfBirth;
        user.EmailConfirmed = model.EmailConfirmed;
        user.LastModifiedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            // Update DateOfBirth claim if exists
            var dateOfBirthClaim = (await _userManager.GetClaimsAsync(user))
                .FirstOrDefault(c => c.Type == "DateOfBirth");
            
            if (dateOfBirthClaim != null && model.DateOfBirth.HasValue)
            {
                await _userManager.RemoveClaimAsync(user, dateOfBirthClaim);
                await _userManager.AddClaimAsync(user, new Claim("DateOfBirth", model.DateOfBirth.Value.ToString("yyyy-MM-dd")));
            }
            else if (model.DateOfBirth.HasValue && dateOfBirthClaim == null)
            {
                await _userManager.AddClaimAsync(user, new Claim("DateOfBirth", model.DateOfBirth.Value.ToString("yyyy-MM-dd")));
            }

            // Update EmailConfirmed claim
            var emailConfirmedClaim = (await _userManager.GetClaimsAsync(user))
                .FirstOrDefault(c => c.Type == "EmailConfirmed");
            
            if (emailConfirmedClaim != null)
            {
                await _userManager.RemoveClaimAsync(user, emailConfirmedClaim);
            }
            
            if (model.EmailConfirmed)
            {
                await _userManager.AddClaimAsync(user, new Claim("EmailConfirmed", "true"));
            }

            _logger.LogInformation("Admin updated user: {UserName}", user.UserName);
            return RedirectToAction(nameof(Details), new { id = user.Id });
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    public async Task<IActionResult> ManageRoles(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var model = new List<RoleAssignmentViewModel>();
        var userRoles = await _userManager.GetRolesAsync(user);
        var allRoles = await _roleManager.Roles.ToListAsync();

        foreach (var role in allRoles)
        {
            model.Add(new RoleAssignmentViewModel
            {
                RoleId = role.Id,
                RoleName = role.Name,
                Description = role.Description,
                IsSelected = userRoles.Contains(role.Name)
            });
        }

        ViewBag.UserId = userId;
        ViewBag.UserName = user.UserName;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManageRoles(List<RoleAssignmentViewModel> model, string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        
        // Remove existing roles
        var result = await _userManager.RemoveFromRolesAsync(user, userRoles);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Failed to remove existing roles.");
            return View(model);
        }

        // Add selected roles
        var selectedRoles = model.Where(x => x.IsSelected).Select(x => x.RoleName);
        result = await _userManager.AddToRolesAsync(user, selectedRoles);
        
        if (result.Succeeded)
        {
            _logger.LogInformation("Admin updated roles for user: {UserName}", user.UserName);
            return RedirectToAction(nameof(Details), new { id = userId });
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    public async Task<IActionResult> ManageClaims(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var existingClaims = await _userManager.GetClaimsAsync(user);
        var model = new ClaimsManagementViewModel
        {
            UserId = userId,
            Claims = existingClaims.Select(c => new UserClaimViewModel { Type = c.Type, Value = c.Value }).ToList()
        };

        ViewBag.UserName = user.UserName;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManageClaims(ClaimsManagementViewModel model)
    {
        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user == null)
        {
            return NotFound();
        }

        // Remove claims marked for deletion
        foreach (var claim in model.Claims.Where(c => c.Delete))
        {
            await _userManager.RemoveClaimAsync(user, new Claim(claim.Type, claim.Value));
            _logger.LogInformation("Removed claim {ClaimType}:{ClaimValue} from user {UserName}", 
                claim.Type, claim.Value, user.UserName);
        }

        // Add new claim if provided
        if (!string.IsNullOrEmpty(model.NewClaimType) && !string.IsNullOrEmpty(model.NewClaimValue))
        {
            await _userManager.AddClaimAsync(user, new Claim(model.NewClaimType, model.NewClaimValue));
            _logger.LogInformation("Added claim {ClaimType}:{ClaimValue} to user {UserName}", 
                model.NewClaimType, model.NewClaimValue, user.UserName);
        }

        return RedirectToAction(nameof(ManageClaims), new { userId = model.UserId });
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var model = new AdminResetPasswordViewModel
        {
            UserId = user.Id,
            Email = user.Email
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(AdminResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user == null)
        {
            return NotFound();
        }

        // Generate a reset token and reset the password
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
        
        if (result.Succeeded)
        {
            _logger.LogInformation("Admin reset password for user: {UserName}", user.UserName);
            TempData["StatusMessage"] = "Password has been reset successfully.";
            return RedirectToAction(nameof(Details), new { id = user.Id });
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LockUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Check if current user is admin to prevent locking themselves
        var currentUserId = _userManager.GetUserId(User);
        if (currentUserId == id)
        {
            TempData["ErrorMessage"] = "You cannot lock your own account.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var endDate = DateTimeOffset.UtcNow.AddYears(100);
        var result = await _userManager.SetLockoutEndDateAsync(user, endDate);
        
        if (result.Succeeded)
        {
            _logger.LogInformation("Admin locked user: {UserName}", user.UserName);
            TempData["StatusMessage"] = "User has been locked successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to lock user.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlockUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var result = await _userManager.SetLockoutEndDateAsync(user, null);
        
        if (result.Succeeded)
        {
            _logger.LogInformation("Admin unlocked user: {UserName}", user.UserName);
            TempData["StatusMessage"] = "User has been unlocked successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to unlock user.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
