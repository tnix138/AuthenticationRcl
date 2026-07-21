using AuthenticationRcl.ViewModels.Base;

namespace AuthenticationRcl.ViewModels.User;

public class UserInfoResponse : ResponseBase
{
    public int UserId { get; set; }                     // شناسه کاربر
    public string Username { get; set; } = string.Empty; // نام کاربری
    public string? Email { get; set; }                  // ایمیل کاربر
    public string? PhoneNumber { get; set; }            // شماره موبایل کاربر
    public string Role { get; set; } = "User";          // نقش کاربر
    public bool IsActive { get; set; }                  // وضعیت فعال بودن
    public bool IsEmailConfirmed { get; set; }          // وضعیت تایید ایمیل
    public bool IsPhoneConfirmed { get; set; }          // وضعیت تایید شماره
    public bool TwoFactorEnabled { get; set; }          // وضعیت 2FA
    public DateTime CreatedAt { get; set; }             // تاریخ ایجاد
    public DateTime? LastLoginAt { get; set; }          // تاریخ آخرین ورود
}

public class UpdateUserRequest
{
    public string? Email { get; set; }                  // ایمیل جدید
    public string? PhoneNumber { get; set; }            // شماره موبایل جدید
    public string? Role { get; set; }                   // نقش جدید
    public bool? IsActive { get; set; }                 // وضعیت فعال/غیرفعال
}

public class UpdateUserResponse : ResponseBase
{
    public int UserId { get; set; }                     // شناسه کاربر
    public bool IsUpdated { get; set; }                 // وضعیت بروزرسانی
}
public class UpdateProfileRequest
{
    public string? Email { get; set; }          // ایمیل جدید
    public string? PhoneNumber { get; set; }    // شماره موبایل جدید
}

public class UpdateProfileResponse : ResponseBase
{
    public bool IsUpdated { get; set; }         // وضعیت بروزرسانی
    public string? Email { get; set; }          // ایمیل جدید
    public string? PhoneNumber { get; set; }    // شماره موبایل جدید
    public bool IsEmailConfirmed { get; set; }  // وضعیت تایید ایمیل
    public bool IsPhoneConfirmed { get; set; }  // وضعیت تایید شماره
}