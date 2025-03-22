using System.ComponentModel.DataAnnotations;

namespace QuanLySanPhamApp.Models.Identity;

public class RefreshTokenModel
{
    [Required]
    public string AccessToken { get; set; } = string.Empty;

    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
