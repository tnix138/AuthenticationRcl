using System.Security.Cryptography;
using System.Text;
using AuthenticationRcl.Models;
using AuthenticationRcl.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BCrypt.Net;

namespace AuthenticationRcl.Services;

#region اینترفیس

/// <summary>
/// سرویس اصلی احراز هویت - اینترفیس
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// ثبت نام کاربر جدید
    /// </summary>
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// ورود کاربر به سیستم
    /// </summary>
    Task<LoginResponse> LoginAsync(LoginRequest request);

    /// <summary>
    /// خروج کاربر از سیستم
    /// </summary>
    Task<LogoutResponse> LogoutAsync(LogoutRequest request);

    /// <summary>
    /// تغییر رمز عبور کاربر
    /// </summary>
    Task<ChangePasswordResponse> ChangePasswordAsync(ChangePasswordRequest request);

    /// <summary>
    /// دریافت اطلاعات کامل کاربر
    /// </summary>
    Task<UserInfoResponse> GetUserInfoAsync(int userId);

    /// <summary>
    /// درخواست بازنشانی رمز عبور (ارسال توکن)
    /// </summary>
    Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request);

    /// <summary>
    /// بازنشانی رمز عبور با استفاده از توکن
    /// </summary>
    Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request);

    /// <summary>
    /// تجدید Access Token با استفاده از Refresh Token
    /// </summary>
    Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
}

#endregion

#region پیاده‌سازی

/// <summary>
/// پیاده‌سازی سرویس احراز هویت
/// </summary>
public class AuthService : IAuthService
{
    #region فیلدها و سازنده

    private readonly AppDbContext _context;
    private readonly ILogger<AuthService> _logger;
    private readonly ITokenService _tokenService;

    /// <summary>
    /// سازنده سرویس احراز هویت
    /// </summary>
    public AuthService(AppDbContext context, ILogger<AuthService> logger, ITokenService tokenService)
    {
        _context = context;
        _logger = logger;
        _tokenService = tokenService;
    }

    #endregion

    #region RegisterAsync - ثبت نام

    /// <summary>
    /// ثبت نام کاربر جدید در سیستم
    /// </summary>
    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // اعتبارسنجی رمز عبور
            if (string.IsNullOrEmpty(request.Password))
            {
                return new RegisterResponse
                {
                    IsSucceded = false,
                    Message = "رمز عبور نمی‌تواند خالی باشد",
                    DevMessage = "رمز عبور اجباری است"
                };
            }

            if (request.Password.Length < 8)
            {
                return new RegisterResponse
                {
                    IsSucceded = false,
                    Message = "رمز عبور باید حداقل ۸ کاراکتر باشد",
                    DevMessage = "رمز عبور باید حداقل ۸ کاراکتر باشد"
                };
            }

            // بررسی یکتایی ایمیل
            if (!string.IsNullOrEmpty(request.Email))
            {
                var existingEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.EmailAddress == request.Email);

                if (existingEmail != null)
                {
                    return new RegisterResponse
                    {
                        IsSucceded = false,
                        Message = "این ایمیل قبلاً ثبت نام کرده است",
                        DevMessage = "ایمیل تکراری است"
                    };
                }
            }

            // بررسی یکتایی شماره موبایل
            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                var existingPhone = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

                if (existingPhone != null)
                {
                    return new RegisterResponse
                    {
                        IsSucceded = false,
                        Message = "این شماره موبایل قبلاً ثبت نام کرده است",
                        DevMessage = "شماره موبایل تکراری است"
                    };
                }
            }

            // بررسی وجود حداقل یکی از ایمیل یا شماره موبایل
            if (string.IsNullOrEmpty(request.Email) && string.IsNullOrEmpty(request.PhoneNumber))
            {
                return new RegisterResponse
                {
                    IsSucceded = false,
                    Message = "حداقل یکی از ایمیل یا شماره موبایل باید وارد شود",
                    DevMessage = "ایمیل یا شماره موبایل الزامی است"
                };
            }

            // هش کردن رمز عبور با BCrypt
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // ایجاد کاربر جدید
            var user = new User
            {
                EmailAddress = request.Email,
                PhoneNumber = request.PhoneNumber,
                Password = hashedPassword,
                Role = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // تولید توکن‌ها
            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // ذخیره Refresh Token در دیتابیس
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(30);
            await _context.SaveChangesAsync();

            _logger.LogInformation("کاربر جدید با ایمیل {Email} ثبت نام کرد", request.Email ?? request.PhoneNumber);

            return new RegisterResponse
            {
                IsSucceded = true,
                Message = "ثبت نام با موفقیت انجام شد",
                DevMessage = "ثبت نام موفق",
                UserId = user.UserId,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 900 // 15 دقیقه
            };
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "خطای پایگاه داده در ثبت نام");
            return new RegisterResponse
            {
                IsSucceded = false,
                Message = "خطا در اتصال به پایگاه داده. لطفاً مجدداً تلاش کنید",
                DevMessage = $"خطای پایگاه داده: {dbEx.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطای سیستمی در ثبت نام");
            return new RegisterResponse
            {
                IsSucceded = false,
                Message = "خطای سیستمی رخ داده است. لطفاً مجدداً تلاش کنید",
                DevMessage = $"خطای سیستمی: {ex.Message}"
            };
        }
    }

    #endregion

    #region LoginAsync - ورود

    /// <summary>
    /// ورود کاربر به سیستم
    /// </summary>
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            // اعتبارسنجی رمز عبور
            if (string.IsNullOrEmpty(request.Password))
            {
                return new LoginResponse
                {
                    IsSucceded = false,
                    Message = "رمز عبور نمی‌تواند خالی باشد",
                    DevMessage = "رمز عبور اجباری است"
                };
            }

            // بررسی وجود حداقل یکی از ایمیل یا شماره موبایل
            if (string.IsNullOrEmpty(request.Email) && string.IsNullOrEmpty(request.PhoneNumber))
            {
                return new LoginResponse
                {
                    IsSucceded = false,
                    Message = "حداقل یکی از ایمیل یا شماره موبایل باید وارد شود",
                    DevMessage = "ایمیل یا شماره موبایل الزامی است"
                };
            }

            // پیدا کردن کاربر
            User? user = null;

            if (!string.IsNullOrEmpty(request.Email))
            {
                user = await _context.Users
                    .FirstOrDefaultAsync(u => u.EmailAddress == request.Email);
            }
            else if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                user = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);
            }

            if (user == null)
            {
                _logger.LogWarning("تلاش ورود ناموفق - کاربر با ایمیل {Email} یا شماره {Phone} یافت نشد",
                    request.Email ?? "null", request.PhoneNumber ?? "null");

                return new LoginResponse
                {
                    IsSucceded = false,
                    Message = "کاربری با این مشخصات یافت نشد",
                    DevMessage = "کاربر پیدا نشد"
                };
            }

            // بررسی قفل بودن کاربر
            if (user.LockoutEndTime.HasValue && user.LockoutEndTime > DateTime.UtcNow)
            {
                return new LoginResponse
                {
                    IsSucceded = false,
                    Message = $"حساب کاربری شما به دلیل تلاش‌های ناموفق تا {user.LockoutEndTime.Value.ToLocalTime():HH:mm} قفل شده است",
                    DevMessage = "User is locked out"
                };
            }

            // بررسی رمز عبور با BCrypt
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                user.FailedLoginAttempts++;
                user.LastLoginAttempt = DateTime.UtcNow;

                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockoutEndTime = DateTime.UtcNow.AddMinutes(15);
                    user.FailedLoginAttempts = 0;
                    await _context.SaveChangesAsync();

                    _logger.LogWarning("کاربر {UserId} به دلیل ۵ تلاش ناموفق قفل شد", user.UserId);

                    return new LoginResponse
                    {
                        IsSucceded = false,
                        Message = "حساب کاربری شما به دلیل ۵ تلاش ناموفق به مدت ۱۵ دقیقه قفل شد",
                        DevMessage = "User locked out after 5 failed attempts"
                    };
                }

                await _context.SaveChangesAsync();

                _logger.LogWarning("تلاش ورود ناموفق برای کاربر {UserId} - {FailedAttempts} تلاش ناموفق",
                    user.UserId, user.FailedLoginAttempts);

                return new LoginResponse
                {
                    IsSucceded = false,
                    Message = $"رمز عبور اشتباه است. {5 - user.FailedLoginAttempts} تلاش دیگر باقی مانده",
                    DevMessage = "رمز عبور نامعتبر"
                };
            }

            // ورود موفق - ریست کردن تلاش‌ها
            user.FailedLoginAttempts = 0;
            user.LockoutEndTime = null;
            user.LastLoginAttempt = DateTime.UtcNow;
            user.LastLoginAt = DateTime.UtcNow;
            user.LastLoginIp = request.IpAddress;

            // تولید توکن‌ها
            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // ذخیره Refresh Token در دیتابیس
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(30);
            await _context.SaveChangesAsync();

            _logger.LogInformation("کاربر {UserId} با موفقیت وارد شد", user.UserId);

            // بررسی فعال بودن کاربر
            if (!user.IsActive)
            {
                return new LoginResponse
                {
                    IsSucceded = false,
                    Message = "حساب کاربری شما غیرفعال شده است. لطفاً با پشتیبانی تماس بگیرید",
                    DevMessage = "کاربر غیرفعال است"
                };
            }

            return new LoginResponse
            {
                IsSucceded = true,
                Message = "ورود با موفقیت انجام شد",
                DevMessage = "ورود موفق",
                UserId = user.UserId,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 900 // 15 دقیقه
            };
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "خطای پایگاه داده در ورود");
            return new LoginResponse
            {
                IsSucceded = false,
                Message = "خطا در اتصال به پایگاه داده. لطفاً مجدداً تلاش کنید",
                DevMessage = $"خطای پایگاه داده: {dbEx.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطای سیستمی در ورود");
            return new LoginResponse
            {
                IsSucceded = false,
                Message = "خطای سیستمی رخ داده است. لطفاً مجدداً تلاش کنید",
                DevMessage = $"خطای سیستمی: {ex.Message}"
            };
        }
    }

    #endregion

    #region LogoutAsync - خروج

    /// <summary>
    /// خروج کاربر از سیستم
    /// </summary>
    public async Task<LogoutResponse> LogoutAsync(LogoutRequest request)
    {
        try
        {
            // پیدا کردن کاربر و پاک کردن Refresh Token
            var user = await _context.Users.FindAsync(request.UserId);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("کاربر {UserId} خارج شد", request.UserId);

            return new LogoutResponse
            {
                IsSucceded = true,
                Message = "خروج با موفقیت انجام شد",
                DevMessage = "Logged out successfully",
                IsLoggedOut = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در خروج کاربر {UserId}", request.UserId);
            return new LogoutResponse
            {
                IsSucceded = false,
                Message = "خطا در خروج از سیستم",
                DevMessage = ex.Message
            };
        }
    }

    #endregion

    #region ChangePasswordAsync - تغییر رمز عبور

    /// <summary>
    /// تغییر رمز عبور کاربر
    /// </summary>
    public async Task<ChangePasswordResponse> ChangePasswordAsync(ChangePasswordRequest request)
    {
        try
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
            {
                return new ChangePasswordResponse
                {
                    IsSucceded = false,
                    Message = "کاربر یافت نشد",
                    DevMessage = "User not found"
                };
            }

            // بررسی رمز فعلی
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
            {
                return new ChangePasswordResponse
                {
                    IsSucceded = false,
                    Message = "رمز عبور فعلی اشتباه است",
                    DevMessage = "Current password is incorrect"
                };
            }

            // بررسی رمز جدید
            if (request.NewPassword.Length < 8)
            {
                return new ChangePasswordResponse
                {
                    IsSucceded = false,
                    Message = "رمز عبور جدید باید حداقل ۸ کاراکتر باشد",
                    DevMessage = "New password must be at least 8 characters"
                };
            }

            // هش و ذخیره رمز جدید
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("رمز عبور کاربر {UserId} تغییر کرد", user.UserId);

            return new ChangePasswordResponse
            {
                IsSucceded = true,
                Message = "رمز عبور با موفقیت تغییر کرد",
                DevMessage = "Password changed successfully",
                IsChanged = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تغییر رمز عبور کاربر {UserId}", request.UserId);
            return new ChangePasswordResponse
            {
                IsSucceded = false,
                Message = "خطا در تغییر رمز عبور",
                DevMessage = ex.Message
            };
        }
    }

    #endregion

    #region GetUserInfoAsync - دریافت اطلاعات کاربر

    /// <summary>
    /// دریافت اطلاعات کامل کاربر
    /// </summary>
    public async Task<UserInfoResponse> GetUserInfoAsync(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new UserInfoResponse
                {
                    IsSucceded = false,
                    Message = "کاربر یافت نشد",
                    DevMessage = "User not found"
                };
            }

            return new UserInfoResponse
            {
                IsSucceded = true,
                Message = "اطلاعات کاربر دریافت شد",
                DevMessage = "User info retrieved",
                UserId = user.UserId,
                Email = user.EmailAddress,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت اطلاعات کاربر {UserId}", userId);
            return new UserInfoResponse
            {
                IsSucceded = false,
                Message = "خطا در دریافت اطلاعات کاربر",
                DevMessage = ex.Message
            };
        }
    }

    #endregion

    #region ForgotPasswordAsync - فراموشی رمز

    /// <summary>
    /// درخواست بازنشانی رمز عبور
    /// </summary>
    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        try
        {
            User? user = null;

            if (!string.IsNullOrEmpty(request.Email))
            {
                user = await _context.Users
                    .FirstOrDefaultAsync(u => u.EmailAddress == request.Email);
            }
            else if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                user = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);
            }

            if (user == null)
            {
                return new ForgotPasswordResponse
                {
                    IsSucceded = false,
                    Message = "کاربری با این مشخصات یافت نشد",
                    DevMessage = "User not found"
                };
            }

            // تولید توکن تصادفی
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            // ذخیره توکن با زمان انقضای ۱ ساعت
            user.ResetPasswordToken = token;
            user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(1);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("توکن بازنشانی رمز برای کاربر {UserId} تولید شد", user.UserId);

            return new ForgotPasswordResponse
            {
                IsSucceded = true,
                Message = "لینک بازنشانی رمز عبور به ایمیل شما ارسال شد",
                DevMessage = $"Reset token: {token}",
                IsSent = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در فراموشی رمز عبور");
            return new ForgotPasswordResponse
            {
                IsSucceded = false,
                Message = "خطا در ارسال لینک بازنشانی رمز عبور",
                DevMessage = ex.Message
            };
        }
    }

    #endregion

    #region ResetPasswordAsync - بازنشانی رمز

    /// <summary>
    /// بازنشانی رمز عبور با استفاده از توکن
    /// </summary>
    public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        try
        {
            User? user = null;

            if (!string.IsNullOrEmpty(request.Email))
            {
                user = await _context.Users
                    .FirstOrDefaultAsync(u => u.EmailAddress == request.Email);
            }
            else if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                user = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);
            }

            if (user == null)
            {
                return new ResetPasswordResponse
                {
                    IsSucceded = false,
                    Message = "کاربری با این مشخصات یافت نشد",
                    DevMessage = "User not found"
                };
            }

            // بررسی توکن
            if (user.ResetPasswordToken != request.Token)
            {
                return new ResetPasswordResponse
                {
                    IsSucceded = false,
                    Message = "توکن نامعتبر است",
                    DevMessage = "Invalid token"
                };
            }

            // بررسی انقضای توکن
            if (user.ResetPasswordTokenExpiry < DateTime.UtcNow)
            {
                return new ResetPasswordResponse
                {
                    IsSucceded = false,
                    Message = "توکن منقضی شده است. مجدداً درخواست کنید",
                    DevMessage = "Token expired"
                };
            }

            // بررسی رمز جدید
            if (request.NewPassword.Length < 8)
            {
                return new ResetPasswordResponse
                {
                    IsSucceded = false,
                    Message = "رمز عبور جدید باید حداقل ۸ کاراکتر باشد",
                    DevMessage = "Password must be at least 8 characters"
                };
            }

            // هش و ذخیره رمز جدید
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.ResetPasswordToken = null;
            user.ResetPasswordTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("رمز عبور کاربر {UserId} با موفقیت بازنشانی شد", user.UserId);

            return new ResetPasswordResponse
            {
                IsSucceded = true,
                Message = "رمز عبور با موفقیت بازنشانی شد",
                DevMessage = "Password reset successfully",
                IsReset = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در بازنشانی رمز عبور");
            return new ResetPasswordResponse
            {
                IsSucceded = false,
                Message = "خطا در بازنشانی رمز عبور",
                DevMessage = ex.Message
            };
        }
    }

    #endregion

    #region RefreshTokenAsync - تجدید توکن

    /// <summary>
    /// تجدید Access Token با استفاده از Refresh Token
    /// </summary>
    public async Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        try
        {
            // پیدا کردن کاربر با Refresh Token
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);

            if (user == null)
            {
                return new RefreshTokenResponse
                {
                    IsSucceded = false,
                    Message = "توکن نامعتبر است",
                    DevMessage = "Invalid refresh token"
                };
            }

            // بررسی انقضای Refresh Token
            if (user.RefreshTokenExpiryTime < DateTime.UtcNow)
            {
                return new RefreshTokenResponse
                {
                    IsSucceded = false,
                    Message = "توکن منقضی شده است. مجدداً وارد شوید",
                    DevMessage = "Refresh token expired"
                };
            }

            // تولید توکن‌های جدید
            var newAccessToken = _tokenService.GenerateAccessToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            // ذخیره Refresh Token جدید در دیتابیس
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(30);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("توکن کاربر {UserId} با موفقیت تجدید شد", user.UserId);

            return new RefreshTokenResponse
            {
                IsSucceded = true,
                Message = "توکن با موفقیت تجدید شد",
                DevMessage = "Token refreshed successfully",
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = 900 // 15 دقیقه
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تجدید توکن");
            return new RefreshTokenResponse
            {
                IsSucceded = false,
                Message = "خطا در تجدید توکن",
                DevMessage = ex.Message
            };
        }
    }

    #endregion

    #region متد کمکی (برای تست)

    private string HashPasswordLegacy(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    #endregion
}

#endregion