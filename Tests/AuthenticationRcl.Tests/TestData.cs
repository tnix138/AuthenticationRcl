using AuthenticationRcl.Models;
using AuthenticationRcl.ViewModels.Auth;
using AuthenticationRcl.ViewModels.OTP;
using AuthenticationRcl.ViewModels.User;
using BCrypt.Net;

namespace AuthenticationRcl.Tests;

public static class TestData
{
    // ثبت نام
    public static RegisterRequest Register(string username = null, string email = null, string phone = null, string pass = "Test@123")
        => new()
        {
            Username = username ?? $"user{Guid.NewGuid():N}".Substring(0, 10),
            Email = email ?? $"test{Guid.NewGuid():N}@example.com",
            PhoneNumber = phone ?? $"0912{Guid.NewGuid():N}".Substring(0, 11),
            Password = pass
        };

    // ورود مرحله اول
    public static LoginStep1Request LoginStep1(string username = null, string pass = "Test@123", string ip = "127.0.0.1")
        => new()
        {
            Username = username ?? $"user{Guid.NewGuid():N}".Substring(0, 10),
            Password = pass,
            IpAddress = ip
        };

    // ورود مرحله دوم
    public static LoginStep2Request LoginStep2(string loginToken = null, string method = "Email")
        => new()
        {
            LoginToken = loginToken ?? "mock-login-token",
            Method = method
        };

    // ورود مرحله سوم
    public static LoginStep3Request LoginStep3(string loginToken = null, string code = "123456")
        => new()
        {
            LoginToken = loginToken ?? "mock-login-token",
            Code = code
        };

    // ساخت کاربر
    public static User User(string username = null, string email = null, string phone = null)
        => new()
        {
            Username = username ?? $"user{Guid.NewGuid():N}".Substring(0, 10),
            EmailAddress = email ?? $"test{Guid.NewGuid():N}@example.com",
            PhoneNumber = phone ?? $"0912{Guid.NewGuid():N}".Substring(0, 11),
            Password = BCrypt.Net.BCrypt.HashPassword("Test@123"),
            IsActive = true
        };

    // تغییر رمز عبور
    public static ChangePasswordRequest ChangePassword(int userId, string current = "Test@123", string newPass = "New@123")
        => new()
        {
            UserId = userId,
            CurrentPassword = current,
            NewPassword = newPass
        };

    // فراموشی رمز
    public static ForgotPasswordRequest ForgotPassword(string email = null, string phone = null)
        => new()
        {
            Email = email,
            PhoneNumber = phone
        };

    // بازنشانی رمز
    public static ResetPasswordRequest ResetPassword(string email, string token, string newPass = "New@123")
        => new()
        {
            Email = email,
            Token = token,
            NewPassword = newPass
        };

    // تجدید توکن
    public static RefreshTokenRequest RefreshToken(string token)
        => new()
        {
            RefreshToken = token
        };

    // ارسال OTP
    public static SendOTPRequest SendOTP(int userId, string type = "Login", string channel = "Email")
        => new()
        {
            UserId = userId,
            Type = type,
            Channel = channel
        };

    // تایید OTP
    public static VerifyOTPRequest VerifyOTP(int userId, string code, string type = "Login")
        => new()
        {
            UserId = userId,
            Code = code,
            Type = type
        };

    // بروزرسانی پروفایل
    public static UpdateProfileRequest UpdateProfile(string email = null, string phone = null)
        => new()
        {
            Email = email,
            PhoneNumber = phone
        };

    // بروزرسانی کاربر (ادمین)
    public static UpdateUserRequest UpdateUser(string email = null, string phone = null, string role = null, bool? isActive = null)
        => new()
        {
            Email = email,
            PhoneNumber = phone,
            Role = role,
            IsActive = isActive
        };

    // خروج
    public static LogoutRequest Logout(int userId)
        => new()
        {
            UserId = userId
        };
}