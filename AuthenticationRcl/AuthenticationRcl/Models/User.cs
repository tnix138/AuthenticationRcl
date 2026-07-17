using System.ComponentModel.DataAnnotations;

namespace AuthenticationRcl.Models;

/// <summary>
/// موجودیت کاربر اصلی سیستم
/// </summary>
public class User
{
    #region Properties

    /// <summary>
    /// شناسه یکتای کاربر - کلید اصلی
    /// </summary>
    [Key]
    public int UserId { get; set; }

    /// <summary>
    /// آدرس ایمیل کاربر - یکتا
    /// </summary>
    public string? EmailAddress { get; set; }

    /// <summary>
    /// شماره موبایل کاربر - یکتا
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// رمز عبور کاربر - به صورت هش شده در دیتابیس ذخیره می‌شود
    /// </summary>
    public required string Password { get; set; }

    /// <summary>
    /// نقش کاربر در سیستم
    /// </summary>
    public string Role { get; set; } = "User";

    /// <summary>
    /// وضعیت فعال بودن حساب کاربری
    /// </summary>
    public bool IsActive { get; set; } = true;

    #endregion

    #region Security Fields

    /// <summary>
    /// تعداد تلاش‌های ناموفق برای ورود
    /// </summary>
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>
    /// زمان پایان قفل بودن حساب کاربری
    /// </summary>
    public DateTime? LockoutEndTime { get; set; }

    /// <summary>
    /// زمان آخرین تلاش برای ورود (موفق یا ناموفق)
    /// </summary>
    public DateTime? LastLoginAttempt { get; set; }

    #endregion

    #region Refresh Token Fields

    /// <summary>
    /// توکن بازنشانی (Refresh Token)
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// زمان انقضای توکن بازنشانی
    /// </summary>
    public DateTime? RefreshTokenExpiryTime { get; set; }

    #endregion

    #region Password Reset Fields

    /// <summary>
    /// توکن بازنشانی رمز عبور
    /// </summary>
    public string? ResetPasswordToken { get; set; }

    /// <summary>
    /// زمان انقضای توکن بازنشانی رمز عبور
    /// </summary>
    public DateTime? ResetPasswordTokenExpiry { get; set; }

    #endregion

    #region Audit Fields

    /// <summary>
    /// تاریخ ایجاد کاربر
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// تاریخ آخرین بروزرسانی اطلاعات کاربر
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// تاریخ آخرین ورود موفق کاربر
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// آی‌پی آخرین ورود موفق کاربر
    /// </summary>
    public string? LastLoginIp { get; set; }

    #endregion
}