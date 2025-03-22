using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySanPhamApp.Models.Identity;

namespace QuanLySanPhamApp.Controllers;

[Authorize(Roles = "Admin")]
public class RoleManagementController : Controller
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<RoleManagementController> _logger;

    public RoleManagementController(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger<RoleManagementController> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        return View(roles);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ApplicationRole role)
    {
        if (ModelState.IsValid)
        {
            var result = await _roleManager.CreateAsync(role);
            if (result.Succeeded)
            {
                _logger.LogInformation("Admin created a new role: {RoleName}", role.Name);
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(role);
    }

    public async Task<IActionResult> Edit(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            return NotFound();
        }

        return View(role);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ApplicationRole role)
    {
        if (ModelState.IsValid)
        {
            var existingRole = await _roleManager.FindByIdAsync(role.Id);
            if (existingRole == null)
            {
                return NotFound();
            }

            existingRole.Name = role.Name;
            existingRole.Description = role.Description;
            existingRole.LastModifiedAt = DateTime.UtcNow;

            var result = await _roleManager.UpdateAsync(existingRole);
            if (result.Succeeded)
            {
                _logger.LogInformation("Admin updated role: {RoleName}", role.Name);
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(role);
    }

    public async Task<IActionResult> Details(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            return NotFound();
        }

        var usersInRole = new List<ApplicationUser>();
        var allUsers = await _userManager.Users.ToListAsync();

        foreach (var user in allUsers)
        {
            if (await _userManager.IsInRoleAsync(user, role.Name))
            {
                usersInRole.Add(user);
            }
        }

        ViewBag.UsersInRole = usersInRole;
        return View(role);
    }

    public async Task<IActionResult> Delete(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            return NotFound();
        }

        return View(role);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            return NotFound();
        }

        // Check if role is in use before deleting
        var usersInRole = new List<ApplicationUser>();
        var allUsers = await _userManager.Users.ToListAsync();

        foreach (var user in allUsers)
        {
            if (await _userManager.IsInRoleAsync(user, role.Name))
            {
                usersInRole.Add(user);
            }
        }

        if (usersInRole.Any())
        {
            ModelState.AddModelError(string.Empty, "Cannot delete role because it is assigned to users.");
            return View(role);
        }

        var result = await _roleManager.DeleteAsync(role);
        if (result.Succeeded)
        {
            _logger.LogInformation("Admin deleted role: {RoleName}", role.Name);
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(role);
    }

    public async Task<IActionResult> ManageUsers(string roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null)
        {
            return NotFound();
        }

        var model = new List<UserRoleViewModel>();
        var allUsers = await _userManager.Users.ToListAsync();

        foreach (var user in allUsers)
        {
            var viewModel = new UserRoleViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FullName = user.FullName,
                IsSelected = await _userManager.IsInRoleAsync(user, role.Name)
            };

            model.Add(viewModel);
        }

        ViewBag.RoleId = roleId;
        ViewBag.RoleName = role.Name;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManageUsers(List<UserRoleViewModel> model, string roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null)
        {
            return NotFound();
        }

        for (int i = 0; i < model.Count; i++)
        {
            var user = await _userManager.FindByIdAsync(model[i].UserId);
            if (user == null)
            {
                continue;
            }

            var isInRole = await _userManager.IsInRoleAsync(user, role.Name);
            
            if (model[i].IsSelected && !isInRole)
            {
                await _userManager.AddToRoleAsync(user, role.Name);
                _logger.LogInformation("Added user {UserName} to role {RoleName}", user.UserName, role.Name);
            }
            else if (!model[i].IsSelected && isInRole)
            {
                await _userManager.RemoveFromRoleAsync(user, role.Name);
                _logger.LogInformation("Removed user {UserName} from role {RoleName}", user.UserName, role.Name);
            }
        }

        return RedirectToAction(nameof(Details), new { id = roleId });
    }
}
