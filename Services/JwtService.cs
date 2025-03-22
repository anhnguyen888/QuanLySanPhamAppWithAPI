using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using QuanLySanPhamApp.Models.Identity;

namespace QuanLySanPhamApp.Services;

public class JwtService
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;

    public JwtService(IConfiguration configuration, UserManager<ApplicationUser> userManager)
    {
        _configuration = configuration;
        _userManager = userManager;
    }

    public async Task<TokenModel> GenerateJwtToken(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim("uid", user.Id)
        };

        // Add roles as claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add custom claims
        var userClaims = await _userManager.GetClaimsAsync(user);
        claims.AddRange(userClaims);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"] ?? throw new InvalidOperationException("JWT:SecretKey is not configured")));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JWT:ExpirationInMinutes"] ?? "60"));

        var token = new JwtSecurityToken(
            issuer: _configuration["JWT:ValidIssuer"],
            audience: _configuration["JWT:ValidAudience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        var refreshToken = GenerateRefreshToken();
        await StoreRefreshToken(user, refreshToken);

        return new TokenModel
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            RefreshToken = refreshToken,
            Expiration = expires
        };
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private async Task StoreRefreshToken(ApplicationUser user, string refreshToken)
    {
        user.SecurityStamp = Guid.NewGuid().ToString();
        await _userManager.UpdateSecurityStampAsync(user);
        
        // Store refresh token in user claims
        var existingRefreshTokenClaim = await _userManager.GetClaimsAsync(user);
        var refreshTokenClaim = existingRefreshTokenClaim.FirstOrDefault(c => c.Type == "RefreshToken");
        
        if (refreshTokenClaim != null)
        {
            await _userManager.RemoveClaimAsync(user, refreshTokenClaim);
        }
        
        await _userManager.AddClaimAsync(user, new Claim("RefreshToken", refreshToken));
        await _userManager.AddClaimAsync(user, new Claim("RefreshTokenExpiry", DateTime.UtcNow.AddDays(7).ToString()));
    }

    public async Task<TokenModel> RefreshToken(string accessToken, string refreshToken)
    {
        var principal = GetPrincipalFromExpiredToken(accessToken);
        if (principal == null)
        {
            throw new SecurityTokenException("Invalid access token");
        }

        var userId = principal.FindFirst("uid")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new SecurityTokenException("Invalid access token");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new SecurityTokenException("User not found");
        }

        var userClaims = await _userManager.GetClaimsAsync(user);
        var storedRefreshToken = userClaims.FirstOrDefault(c => c.Type == "RefreshToken")?.Value;
        var refreshTokenExpiry = userClaims.FirstOrDefault(c => c.Type == "RefreshTokenExpiry")?.Value;

        if (storedRefreshToken != refreshToken || string.IsNullOrEmpty(refreshTokenExpiry))
        {
            throw new SecurityTokenException("Invalid refresh token");
        }

        if (DateTime.Parse(refreshTokenExpiry) < DateTime.UtcNow)
        {
            throw new SecurityTokenException("Refresh token expired");
        }

        return await GenerateJwtToken(user);
    }

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"])),
            ValidIssuer = _configuration["JWT:ValidIssuer"],
            ValidAudience = _configuration["JWT:ValidAudience"],
            ValidateLifetime = false // We don't care about the token's expiration
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
        
        if (securityToken is not JwtSecurityToken jwtSecurityToken || 
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token");
        }

        return principal;
    }
}
