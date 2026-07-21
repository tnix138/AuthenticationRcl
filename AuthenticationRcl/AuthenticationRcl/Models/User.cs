using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthenticationRcl.Models;

public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int UserId { get; set; }

    [MaxLength(50)]
    [Required]
    public string Username { get; set; } = string.Empty;      // نام کاربری یکتا

    [MaxLength(256)]
    public string? EmailAddress { get; set; }                 // ایمیل کاربر

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }                  // شماره موبایل کاربر

    public bool IsEmailConfirmed { get; set; } = false;       // وضعیت تایید ایمیل

    public bool IsPhoneConfirmed { get; set; } = false;       // وضعیت تایید شماره موبایل

    [MaxLength(255)]
    [Required]
    public string Password { get; set; } = string.Empty;      // رمز عبور هش شده

    [MaxLength(50)]
    public string Role { get; set; } = "User";                // نقش کاربر در سیستم

    public bool IsActive { get; set; } = true;                // وضعیت فعال بودن حساب

    public bool TwoFactorEnabled { get; set; } = false;       // فعال بودن احراز هویت دو مرحله ای

    public string? TwoFactorSecret { get; set; }              // کلید مخفی TOTP

    public string? TwoFactorMethod { get; set; }              // روش احراز دو مرحله ای

    public string? LoginToken { get; set; }                   // توکن موقت ورود

    public DateTime? LoginTokenExpiry { get; set; }           // زمان انقضای توکن موقت

    public int FailedLoginAttempts { get; set; } = 0;         // تعداد تلاش های ناموفق

    public DateTime? LockoutEndTime { get; set; }             // زمان پایان قفل بودن حساب

    public DateTime? LastLoginAttempt { get; set; }           // زمان آخرین تلاش برای ورود

    public string? RefreshToken { get; set; }                 // توکن بازنشانی

    public DateTime? RefreshTokenExpiryTime { get; set; }     // زمان انقضای توکن بازنشانی

    public string? ResetPasswordToken { get; set; }           // توکن بازنشانی رمز عبور

    public DateTime? ResetPasswordTokenExpiry { get; set; }   // زمان انقضای توکن بازنشانی رمز

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // تاریخ ایجاد

    public DateTime? UpdatedAt { get; set; }                  // تاریخ آخرین بروزرسانی

    public DateTime? LastLoginAt { get; set; }                // تاریخ آخرین ورود موفق

    public string? LastLoginIp { get; set; }                  // آی پی آخرین ورود

    // روابط
    public ICollection<UserOTP> OTPs { get; set; } = new List<UserOTP>();              // لیست کدهای تایید

    public ICollection<UserBackupCode> BackupCodes { get; set; } = new List<UserBackupCode>(); // لیست کدهای پشتیبان

    public ICollection<UserDevice> Devices { get; set; } = new List<UserDevice>();     // لیست دستگاه‌ها
}