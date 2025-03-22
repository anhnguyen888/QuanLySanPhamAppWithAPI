using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySanPhamApp.Models.Identity;

namespace QuanLySanPhamApp.Controllers.Api;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class RoleController : ControllerBase
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<RoleController> _logger;

    public RoleController(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger<RoleController> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        return Ok(roles);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRole(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            return NotFound(new { message = "Role not found" });
        }

        // Get users in role
        var usersInRole = new List<object>();
        var allUsers = await _userManager.Users.ToListAsync();

        foreach (var user in allUsers)
        {
            if (await _userManager.IsInRoleAsync(user, role.Name))
            {
                usersInRole.Add(new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    FullName = user.FullName
                });
            }
        }

        return Ok(new
        {
            role.Id,
            role.Name,
            role.Description,
            role.CreatedAt,
            role.LastModifiedAt,
            UsersInRole = usersInRole
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] ApplicationRole role)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _roleManager.CreateAsync(role);
        if (result.Succeeded)
        {
            _logger.LogInformation("Admin created a new role: {RoleName}", role.Name);
            return CreatedAtAction(nameof(GetRole), new { id = role.Id }, role);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return BadRequest(ModelState);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRole(string id, [FromBody] ApplicationRole role)
    {
        if (id != role.Id)
        {
            return BadRequest(new { message = "Role ID mismatch" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existingRole = await _roleManager.FindByIdAsync(id);
        if (existingRole == null)
        {
            return NotFound(new { message = "Role not found" });
        }

        existingRole.Name = role.Name;
        existingRole.Description = role.Description;
        existingRole.LastModifiedAt = DateTime.UtcNow;

        var result = await _roleManager.UpdateAsync(existingRole);
        if (result.Succeeded)
        {
            _logger.LogInformation("Admin updated role: {RoleName}", role.Name);
            return Ok(existingRole);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return BadRequest(ModelState);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRole(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            return NotFound(new { message = "Role not found" });
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
            return BadRequest(new { message = "Cannot delete role because it is assigned to users" });
        }

        var result = await _roleManager.DeleteAsync(role);
        if (result.Succeeded)
        {
            _logger.LogInformation("Admin deleted role: {RoleName}", role.Name);
            return Ok(new { message = "Role deleted successfully" });
        }

        return BadRequest(new { message = "Failed to delete role", errors = result.Errors.Select(e => e.Description) });
    }

    [HttpGet("{roleId}/users")]
    public async Task<IActionResult> GetUsersInRole(string roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null)
        {
            return NotFound(new { message = "Role not found" });
        }

        var usersInRole = new List<object>();
        var allUsers = await _userManager.Users.ToListAsync();

        foreach (var user in allUsers)
        {
            var isInRole = await _userManager.IsInRoleAsync(user, role.Name);
            usersInRole.Add(new
            {
                user.Id,
                user.UserName,
                user.Email,
                FullName = user.FullName,
                IsInRole = isInRole
            });
        }

        return Ok(usersInRole);
    }

    [HttpPost("{roleId}/users")]
    public async Task<IActionResult> UpdateUsersInRole(string roleId, [FromBody] List<UserRoleViewModel> model)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null)
        {
            return NotFound(new { message = "Role not found" });
        }

        foreach (var userRole in model)
        {
            var user = await _userManager.FindByIdAsync(userRole.UserId);
            if (user == null)
            {
                continue;
            }

            var isInRole = await _userManager.IsInRoleAsync(user, role.Name);
            
            if (userRole.IsSelected && !isInRole)
            {
                await _userManager.AddToRoleAsync(user, role.Name);
                _logger.LogInformation("Added user {UserName} to role {RoleName}", user.UserName, role.Name);
            }
            else if (!userRole.IsSelected && isInRole)
            {
                await _userManager.RemoveFromRoleAsync(user, role.Name);
                _logger.LogInformation("Removed user {UserName} from role {RoleName}", user.UserName, role.Name);
            }
        }

        return Ok(new { message = "Users in role updated successfully" });
    }
}
