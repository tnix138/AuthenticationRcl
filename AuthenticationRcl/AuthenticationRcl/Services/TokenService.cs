using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthenticationRcl.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AuthenticationRcl.Services;

#region اینترفیس

/// <summary>
/// سرویس مدیریت توکن‌های JWT
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// تولید Access Token
    /// </summary>
    /// <param name="user">اطلاعات کاربر</param>
    /// <returns>توکن JWT</returns>
    string GenerateAccessToken(User user);

    /// <summary>
    /// تولید Refresh Token
    /// </summary>
    /// <returns>Refresh Token</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// اعتبارسنجی توکن
    /// </summary>
    /// <param name="token">توکن</param>
    /// <returns>معتبر بودن توکن</returns>
    bool ValidateToken(string token);

    /// <summary>
    /// دریافت UserId از توکن
    /// </summary>
    /// <param name="token">توکن</param>
    /// <returns>شناسه کاربر</returns>
    int? GetUserIdFromToken(string token);
}

#endregion

#region پیاده‌سازی

/// <summary>
/// پیاده‌سازی سرویس مدیریت توکن‌های JWT
/// </summary>
public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly SymmetricSecurityKey _key;
    private readonly TokenValidationParameters _tokenValidationParameters;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;

        var secretKey = _configuration["Jwt:SecretKey"] ?? "DefaultSecretKey12345678901234567890";
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        _tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _key,
            ValidateIssuer = true,
            ValidIssuer = _configuration["Jwt:Issuer"] ?? "https://localhost:7001",
            ValidateAudience = true,
            ValidAudience = _configuration["Jwt:Audience"] ?? "https://localhost:7001",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }

    /// <summary>
    /// تولید Access Token
    /// </summary>
    public string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.EmailAddress ?? string.Empty),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("PhoneNumber", user.PhoneNumber ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "https://localhost:7001",
            audience: _configuration["Jwt:Audience"] ?? "https://localhost:7001",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// تولید Refresh Token
    /// </summary>
    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    /// <summary>
    /// اعتبارسنجی توکن
    /// </summary>
    public bool ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken(token, _tokenValidationParameters, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// دریافت UserId از توکن
    /// </summary>
    public int? GetUserIdFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, _tokenValidationParameters, out _);

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}

#endregion