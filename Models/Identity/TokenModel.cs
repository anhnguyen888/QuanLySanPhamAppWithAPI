namespace QuanLySanPhamApp.Models.Identity;

public class TokenModel
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public string TokenType { get; set; } = "Bearer";
}
