using System.ComponentModel.DataAnnotations;

namespace QuanLySanPhamApp.Models.Identity;

public class ForgotPasswordModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
