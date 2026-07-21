using AuthenticationRcl.ViewModels.Base;

namespace AuthenticationRcl.ViewModels.Auth;

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;        // نام کاربری
    public string? Email { get; set; }                          // ایمیل (اختیاری)
    public string? PhoneNumber { get; set; }                    // شماره موبایل (اختیاری)
    public string Password { get; set; } = string.Empty;        // رمز عبور
}

public class RegisterResponse : ResponseBase
{
    public int UserId { get; set; }                             // شناسه کاربر
    public string Username { get; set; } = string.Empty;        // نام کاربری
    public string? AccessToken { get; set; }                    // توکن دسترسی
    public string? RefreshToken { get; set; }                   // توکن بازنشانی
    public int ExpiresIn { get; set; }                          // مدت اعتبار توکن
    public bool RequiresEmailConfirmation { get; set; }         // نیاز به تایید ایمیل
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;        // نام کاربری
    public string Password { get; set; } = string.Empty;        // رمز عبور
    public string? IpAddress { get; set; }                      // آی پی کاربر
    public string? UserAgent { get; set; }                      // مرورگر کاربر
}

public class LoginResponse : ResponseBase
{
    public int UserId { get; set; }                             // شناسه کاربر
    public string Username { get; set; } = string.Empty;        // نام کاربری
    public string? Email { get; set; }                          // ایمیل کاربر
    public string? PhoneNumber { get; set; }                    // شماره موبایل کاربر
    public string? AccessToken { get; set; }                    // توکن دسترسی
    public string? RefreshToken { get; set; }                   // توکن بازنشانی
    public int ExpiresIn { get; set; }                          // مدت اعتبار توکن
    public bool RequiresTwoFactor { get; set; }                 // نیاز به 2FA
}

public class ChangePasswordRequest
{
    public int UserId { get; set; }                             // شناسه کاربر
    public string CurrentPassword { get; set; } = string.Empty; // رمز فعلی
    public string NewPassword { get; set; } = string.Empty;     // رمز جدید
}

public class ChangePasswordResponse : ResponseBase
{
    public bool IsChanged { get; set; }                         // وضعیت تغییر
}

public class ForgotPasswordRequest
{
    public string? Email { get; set; }                          // ایمیل کاربر
    public string? PhoneNumber { get; set; }                    // شماره موبایل کاربر
}

public class ForgotPasswordResponse : ResponseBase
{
    public bool IsSent { get; set; }                            // وضعیت ارسال
}

public class ResetPasswordRequest
{
    public string? Email { get; set; }                          // ایمیل کاربر
    public string? PhoneNumber { get; set; }                    // شماره موبایل کاربر
    public string Token { get; set; } = string.Empty;           // توکن بازنشانی
    public string NewPassword { get; set; } = string.Empty;     // رمز جدید
}

public class ResetPasswordResponse : ResponseBase
{
    public bool IsReset { get; set; }                           // وضعیت بازنشانی
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;    // توکن بازنشانی
}

public class RefreshTokenResponse : ResponseBase
{
    public string? AccessToken { get; set; }                    // توکن دسترسی جدید
    public string? RefreshToken { get; set; }                   // توکن بازنشانی جدید
    public int ExpiresIn { get; set; }                          // مدت اعتبار توکن
}

public class LogoutRequest
{
    public int UserId { get; set; }                             // شناسه کاربر
}

public class LogoutResponse : ResponseBase
{
    public bool IsLoggedOut { get; set; }                       // وضعیت خروج
}

// ================ مدل‌های جدید برای ورود دو مرحله‌ای ================

public class LoginStep1Request
{
    public string Username { get; set; } = string.Empty;        // نام کاربری
    public string Password { get; set; } = string.Empty;        // رمز عبور
    public string? IpAddress { get; set; }                      // آی پی کاربر
    public string? UserAgent { get; set; }                      // مرورگر کاربر
}

public class LoginStep1Response : ResponseBase
{
    public string? LoginToken { get; set; }                     // توکن موقت ورود
    public int ExpiresIn { get; set; } = 300;                   // مدت اعتبار توکن
    public bool RequiresTwoFactor { get; set; }                 // نیاز به 2FA
    public List<TwoFactorMethodInfo> AvailableMethods { get; set; } = new(); // روش‌های موجود
    public UserInfoFor2FA UserInfo { get; set; } = new();       // اطلاعات کاربر
}

public class TwoFactorMethodInfo
{
    public string Type { get; set; } = string.Empty;            // Email, SMS, TOTP
    public string Label { get; set; } = string.Empty;           // برچسب نمایشی
    public bool IsAvailable { get; set; }                       // در دسترس بودن
    public string? MaskedDestination { get; set; }              // مقصد ماسک شده
}

public class UserInfoFor2FA
{
    public int UserId { get; set; }                             // شناسه کاربر
    public string Username { get; set; } = string.Empty;        // نام کاربری
    public bool HasEmail { get; set; }                          // وجود ایمیل تایید شده
    public bool HasPhone { get; set; }                          // وجود شماره تایید شده
    public bool HasTOTP { get; set; }                           // فعال بودن TOTP
}

public class LoginStep2Request
{
    public string LoginToken { get; set; } = string.Empty;      // توکن موقت
    public string Method { get; set; } = string.Empty;          // روش (Email, SMS, TOTP)
}

public class LoginStep2Response : ResponseBase
{
    public string? Method { get; set; }                         // روش انتخاب شده
    public int ExpiresIn { get; set; } = 300;                   // مدت اعتبار
    public string? MaskedDestination { get; set; }              // مقصد ماسک شده
}

public class LoginStep3Request
{
    public string LoginToken { get; set; } = string.Empty;      // توکن موقت
    public string Code { get; set; } = string.Empty;            // کد تایید
    public string? IpAddress { get; set; }                      // آی پی کاربر
}

public class LoginStep3Response : ResponseBase
{
    public int UserId { get; set; }                             // شناسه کاربر
    public string Username { get; set; } = string.Empty;        // نام کاربری
    public string? AccessToken { get; set; }                    // توکن دسترسی
    public string? RefreshToken { get; set; }                   // توکن بازنشانی
    public int ExpiresIn { get; set; }                          // مدت اعتبار توکن
}