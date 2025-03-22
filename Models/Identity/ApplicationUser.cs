using Microsoft.AspNetCore.Identity;
using System;

namespace QuanLySanPhamApp.Models.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? ProfilePicture { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    public string FullName => $"{FirstName} {LastName}";
}
