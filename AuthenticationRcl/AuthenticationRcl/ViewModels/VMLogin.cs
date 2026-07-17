using System.ComponentModel.DataAnnotations;

namespace AuthenticationRcl.ViewModels;

#region ثبت نام (Registration)

/// <summary>
/// بخش مربوط به ثبت نام کاربر جدید
/// </summary>
#region درخواست (Request)

/// <summary>
/// مدل درخواست ثبت نام کاربر جدید
/// </summary>
/// <remarks>
/// حداقل یکی از فیلدهای Email یا PhoneNumber باید پر شود
/// </remarks>
public class RegisterRequest
{
    /// <summary>
    /// آدرس ایمیل کاربر
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// شماره موبایل کاربر
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// رمز عبور کاربر - حداقل ۸ کاراکتر
    /// </summary>
    public required string Password { get; set; }

    /// <summary>
    /// نقش کاربر در سیستم - پیش‌فرض "User"
    /// </summary>
    /// <remarks>
    /// کاربران عادی فقط نقش "User" را دارند
    /// تغییر نقش فقط توسط ادمین انجام می‌شود
    /// </remarks>
    public string Role { get; set; } = "User";
}

#endregion

#region پاسخ (Response)

/// <summary>
/// مدل پاسخ ثبت نام کاربر
/// </summary>
public class RegisterResponse : ResponseBase
{
    /// <summary>
    /// شناسه کاربر ثبت نام شده
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// توکن دسترسی JWT
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// توکن بازنشانی
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// زمان انقضای توکن به ثانیه
    /// </summary>
    public int ExpiresIn { get; set; } = 900; // 15 دقیقه
}

#endregion

#endregion

#region ورود (Login)

/// <summary>
/// بخش مربوط به ورود کاربر
/// </summary>
#region درخواست (Request)

/// <summary>
/// مدل درخواست ورود کاربر
/// </summary>
/// <remarks>
/// حداقل یکی از فیلدهای Email یا PhoneNumber باید پر شود
/// </remarks>
public class LoginRequest
{
    /// <summary>
    /// شماره موبایل کاربر
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// آدرس ایمیل کاربر
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// رمز عبور کاربر
    /// </summary>
    public required string Password { get; set; }

    /// <summary>
    /// آی‌پی دستگاه کاربر - به صورت خودکار از درخواست گرفته می‌شود
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// مرورگر و سیستم‌عامل کاربر - به صورت خودکار از درخواست گرفته می‌شود
    /// </summary>
    public string? UserAgent { get; set; }
}

#endregion

#region پاسخ (Response)

/// <summary>
/// مدل پاسخ ورود کاربر
/// </summary>
public class LoginResponse : ResponseBase
{
    /// <summary>
    /// شناسه کاربر وارد شده
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// توکن دسترسی JWT
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// توکن بازنشانی
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// زمان انقضای توکن به ثانیه
    /// </summary>
    public int ExpiresIn { get; set; } = 900; // 15 دقیقه
}

#endregion

#endregion

#region تغییر رمز عبور (Change Password)

/// <summary>
/// بخش مربوط به تغییر رمز عبور کاربر
/// </summary>
#region درخواست (Request)

/// <summary>
/// مدل درخواست تغییر رمز عبور
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// شناسه کاربر
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// رمز عبور فعلی کاربر - برای تأیید هویت
    /// </summary>
    public required string CurrentPassword { get; set; }

    /// <summary>
    /// رمز عبور جدید - حداقل ۸ کاراکتر
    /// </summary>
    public required string NewPassword { get; set; }
}

#endregion

#region پاسخ (Response)

/// <summary>
/// مدل پاسخ تغییر رمز عبور
/// </summary>
public class ChangePasswordResponse : ResponseBase
{
    /// <summary>
    /// وضعیت تغییر رمز عبور - موفقیت‌آمیز بودن
    /// </summary>
    public bool IsChanged { get; set; }
}

#endregion

#endregion

#region خروج از سیستم (Logout)

/// <summary>
/// بخش مربوط به خروج کاربر از سیستم
/// </summary>
#region درخواست (Request)

/// <summary>
/// مدل درخواست خروج از سیستم
/// </summary>
public class LogoutRequest
{
    /// <summary>
    /// شناسه کاربر خارج شده
    /// </summary>
    public int UserId { get; set; }
}

#endregion

#region پاسخ (Response)

/// <summary>
/// مدل پاسخ خروج از سیستم
/// </summary>
public class LogoutResponse : ResponseBase
{
    /// <summary>
    /// وضعیت خروج - موفقیت‌آمیز بودن
    /// </summary>
    public bool IsLoggedOut { get; set; }
}

#endregion

#endregion

#region اطلاعات کاربر (User Info)

/// <summary>
/// بخش مربوط به دریافت اطلاعات کاربر
/// </summary>
#region درخواست (Request)

/// <summary>
/// مدل درخواست دریافت اطلاعات کاربر
/// </summary>
public class UserInfoRequest
{
    /// <summary>
    /// شناسه کاربر مورد نظر
    /// </summary>
    public int UserId { get; set; }
}

#endregion

#region پاسخ (Response)

/// <summary>
/// مدل پاسخ دریافت اطلاعات کاربر
/// </summary>
public class UserInfoResponse : ResponseBase
{
    /// <summary>
    /// شناسه کاربر
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// آدرس ایمیل کاربر
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// شماره موبایل کاربر
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// نقش کاربر در سیستم
    /// </summary>
    public string Role { get; set; } = "User";

    /// <summary>
    /// وضعیت فعال بودن حساب کاربری
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// تاریخ ثبت‌نام کاربر
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// تاریخ آخرین ورود موفق کاربر
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
}

#endregion

#endregion

#region فراموشی رمز عبور (Forgot/Reset Password)

/// <summary>
/// بخش مربوط به فراموشی و بازنشانی رمز عبور
/// </summary>
#region درخواست (Request)

/// <summary>
/// مدل درخواست ارسال لینک بازنشانی رمز عبور
/// </summary>
public class ForgotPasswordRequest
{
    /// <summary>
    /// آدرس ایمیل کاربر - حداقل یکی از Email یا PhoneNumber باید پر شود
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// شماره موبایل کاربر - حداقل یکی از Email یا PhoneNumber باید پر شود
    /// </summary>
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// مدل درخواست بازنشانی رمز عبور با توکن
/// </summary>
public class ResetPasswordRequest
{
    /// <summary>
    /// آدرس ایمیل کاربر
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// شماره موبایل کاربر
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// رمز عبور جدید - حداقل ۸ کاراکتر
    /// </summary>
    public required string NewPassword { get; set; }

    /// <summary>
    /// توکن بازنشانی رمز عبور - از طریق ایمیل یا پیامک ارسال می‌شود
    /// </summary>
    /// <remarks>توکن باید با مقدار ذخیره شده در دیتابیس مطابقت داشته باشد</remarks>
    public required string Token { get; set; }
}

#endregion

#region پاسخ (Response)

/// <summary>
/// مدل پاسخ ارسال لینک بازنشانی رمز عبور
/// </summary>
public class ForgotPasswordResponse : ResponseBase
{
    /// <summary>
    /// وضعیت ارسال - موفقیت‌آمیز بودن
    /// </summary>
    public bool IsSent { get; set; }
}

/// <summary>
/// مدل پاسخ بازنشانی رمز عبور
/// </summary>
public class ResetPasswordResponse : ResponseBase
{
    /// <summary>
    /// وضعیت بازنشانی - موفقیت‌آمیز بودن
    /// </summary>
    public bool IsReset { get; set; }
}

#endregion

#endregion

#region بروزرسانی کاربر (Update User)

/// <summary>
/// بخش مربوط به بروزرسانی اطلاعات کاربر
/// </summary>
#region درخواست (Request)

/// <summary>
/// مدل درخواست بروزرسانی اطلاعات کاربر
/// </summary>
public class UpdateUserRequest
{
    /// <summary>
    /// آدرس ایمیل جدید کاربر
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// شماره موبایل جدید کاربر
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// نقش جدید کاربر (فقط ادمین)
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// وضعیت فعال بودن کاربر (فقط ادمین)
    /// </summary>
    public bool? IsActive { get; set; }
}

#endregion

#region پاسخ (Response)

/// <summary>
/// مدل پاسخ بروزرسانی اطلاعات کاربر
/// </summary>
public class UpdateUserResponse : ResponseBase
{
    /// <summary>
    /// شناسه کاربر بروزرسانی شده
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// آیا اطلاعات تغییر کرد؟
    /// </summary>
    public bool IsUpdated { get; set; }
}

#endregion

#endregion

#region Refresh Token (تجدید توکن)

/// <summary>
/// بخش مربوط به تجدید توکن
/// </summary>
#region درخواست (Request)

/// <summary>
/// مدل درخواست تجدید توکن
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// Refresh Token برای دریافت توکن جدید
    /// </summary>
    public required string RefreshToken { get; set; }
}

#endregion

#region پاسخ (Response)

/// <summary>
/// مدل پاسخ تجدید توکن
/// </summary>
public class RefreshTokenResponse : ResponseBase
{
    /// <summary>
    /// توکن دسترسی جدید JWT
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// توکن بازنشانی جدید
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// زمان انقضای توکن به ثانیه
    /// </summary>
    public int ExpiresIn { get; set; } = 900; // 15 دقیقه
}

#endregion

#endregion