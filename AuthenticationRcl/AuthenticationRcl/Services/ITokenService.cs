using AuthenticationRcl.Models;

namespace AuthenticationRcl.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user);          // تولید توکن دسترسی
    string GenerateRefreshToken();                  // تولید توکن بازنشانی
    bool ValidateToken(string token);               // اعتبارسنجی توکن
    int? GetUserIdFromToken(string token);          // دریافت شناسه کاربر از توکن
    int GetAccessTokenExpirySeconds();              // دریافت زمان انقضای توکن
}