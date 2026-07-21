using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthenticationRcl.Models;
using AuthenticationRcl.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthenticationRcl.Services;

public class TokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly SymmetricSecurityKey _key;
    private readonly TokenValidationParameters _validationParams;

    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));

        _validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _key,
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(5)
        };
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),                    // ✅ اضافه شد
            new Claim("Username", user.Username),                         // ✅ اضافه شد
            new Claim(ClaimTypes.Email, user.EmailAddress ?? string.Empty),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("PhoneNumber", user.PhoneNumber ?? string.Empty),
            new Claim("TwoFactorEnabled", user.TwoFactorEnabled.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddSeconds(_options.AccessTokenExpirySeconds),
            signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public bool ValidateToken(string token)
    {
        try
        {
            new JwtSecurityTokenHandler().ValidateToken(token, _validationParams, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public int? GetUserIdFromToken(string token)
    {
        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, _validationParams, out _);
            var claim = principal.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : null;
        }
        catch
        {
            return null;
        }
    }

    public int GetAccessTokenExpirySeconds() => _options.AccessTokenExpirySeconds;
}