using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLySanPhamApp.Models.Identity;
using System.Security.Claims;

namespace QuanLySanPhamApp.Controllers.Api;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class UserController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<UserController> _logger;

    public UserController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger<UserController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userManager.Users
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.Email,
                u.FirstName,
                u.LastName,
                FullName = u.FullName,
                u.EmailConfirmed,
                u.PhoneNumber,
                u.PhoneNumberConfirmed,
                u.TwoFactorEnabled,
                u.LockoutEnd,
                u.LockoutEnabled,
                u.CreatedAt,
                u.LastModifiedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var claims = await _userManager.GetClaimsAsync(user);

        return Ok(new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.FirstName,
            user.LastName,
            FullName = user.FullName,
            user.EmailConfirmed,
            user.PhoneNumber,
            user.PhoneNumberConfirmed,
            user.TwoFactorEnabled,
            user.LockoutEnd,
            user.LockoutEnabled,
            user.CreatedAt,
            user.LastModifiedAt,
            Roles = roles,
            Claims = claims.Select(c => new { c.Type, c.Value }).ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] RegisterModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            DateOfBirth = model.DateOfBirth,
            EmailConfirmed = true // Admin-created accounts are auto-confirmed
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            // Add to default User role
            await _userManager.AddToRoleAsync(user, "User");
            
            // Add date of birth claim if provided
            if (model.DateOfBirth.HasValue)
            {
                await _userManager.AddClaimAsync(user, new Claim("DateOfBirth", model.DateOfBirth.Value.ToString("yyyy-MM-dd")));
            }
            
            // Add email confirmed claim
            await _userManager.AddClaimAsync(user, new Claim("EmailConfirmed", "true"));

            _logger.LogInformation("Admin created a new user: {UserName}", user.UserName);
            
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new { 
                user.Id, 
                user.UserName, 
                user.Email, 
                message = "User created successfully" 
            });
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return BadRequest(ModelState);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] EditUserViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest(new { message = "User ID mismatch" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
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
            return Ok(new { message = "User updated successfully" });
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return BadRequest(ModelState);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        // Check if current user is trying to delete themselves
        var currentUserId = _userManager.GetUserId(User);
        if (currentUserId == id)
        {
            return BadRequest(new { message = "You cannot delete your own account" });
        }

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            _logger.LogInformation("Admin deleted user: {UserName}", user.UserName);
            return Ok(new { message = "User deleted successfully" });
        }

        return BadRequest(new { message = "Failed to delete user", errors = result.Errors.Select(e => e.Description) });
    }

    [HttpPost("{id}/roles")]
    public async Task<IActionResult> UpdateUserRoles(string id, [FromBody] List<string> roleNames)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        // Check if all roles exist
        foreach (var roleName in roleNames)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                return BadRequest(new { message = $"Role '{roleName}' does not exist" });
            }
        }

        var existingRoles = await _userManager.GetRolesAsync(user);
        var result = await _userManager.RemoveFromRolesAsync(user, existingRoles);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = "Failed to remove existing roles", errors = result.Errors.Select(e => e.Description) });
        }

        result = await _userManager.AddToRolesAsync(user, roleNames);
        if (result.Succeeded)
        {
            _logger.LogInformation("Admin updated roles for user: {UserName}", user.UserName);
            return Ok(new { message = "User roles updated successfully" });
        }

        return BadRequest(new { message = "Failed to update roles", errors = result.Errors.Select(e => e.Description) });
    }

    [HttpPost("{id}/lock")]
    public async Task<IActionResult> LockUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        // Check if current user is trying to lock themselves
        var currentUserId = _userManager.GetUserId(User);
        if (currentUserId == id)
        {
            return BadRequest(new { message = "You cannot lock your own account" });
        }

        var endDate = DateTimeOffset.UtcNow.AddYears(100);
        var result = await _userManager.SetLockoutEndDateAsync(user, endDate);
        
        if (result.Succeeded)
        {
            _logger.LogInformation("Admin locked user: {UserName}", user.UserName);
            return Ok(new { message = "User locked successfully" });
        }

        return BadRequest(new { message = "Failed to lock user", errors = result.Errors.Select(e => e.Description) });
    }

    [HttpPost("{id}/unlock")]
    public async Task<IActionResult> UnlockUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var result = await _userManager.SetLockoutEndDateAsync(user, null);
        
        if (result.Succeeded)
        {
            _logger.LogInformation("Admin unlocked user: {UserName}", user.UserName);
            return Ok(new { message = "User unlocked successfully" });
        }

        return BadRequest(new { message = "Failed to unlock user", errors = result.Errors.Select(e => e.Description) });
    }

    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(string id, [FromBody] AdminResetPasswordViewModel model)
    {
        if (id != model.UserId)
        {
            return BadRequest(new { message = "User ID mismatch" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        // Generate a reset token and reset the password
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
        
        if (result.Succeeded)
        {
            _logger.LogInformation("Admin reset password for user: {UserName}", user.UserName);
            return Ok(new { message = "Password reset successfully" });
        }

        return BadRequest(new { message = "Failed to reset password", errors = result.Errors.Select(e => e.Description) });
    }
}
