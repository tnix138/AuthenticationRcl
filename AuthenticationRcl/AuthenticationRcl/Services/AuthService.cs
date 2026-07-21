using System.Security.Cryptography;
using AuthenticationRcl.Models;
using AuthenticationRcl.Options;
using AuthenticationRcl.ViewModels.Auth;
using AuthenticationRcl.ViewModels.Base;
using AuthenticationRcl.ViewModels.OTP;
using AuthenticationRcl.ViewModels.TwoFactor;
using AuthenticationRcl.ViewModels.User;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthenticationRcl.Services;

// ============================================================
// اینترفیس سرویس احراز هویت
// ============================================================
public interface IAuthService
{
    // ثبت نام
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);

    // ورود مرحله اول - نام کاربری و رمز عبور
    Task<LoginStep1Response> LoginStep1Async(LoginStep1Request request);

    // ورود مرحله دوم - انتخاب روش دریافت کد
    Task<LoginStep2Response> LoginStep2Async(LoginStep2Request request);

    // ورود مرحله سوم - تایید کد
    Task<LoginStep3Response> LoginStep3Async(LoginStep3Request request);

    // خروج
    Task<LogoutResponse> LogoutAsync(LogoutRequest request);

    // تغییر رمز عبور
    Task<ChangePasswordResponse> ChangePasswordAsync(ChangePasswordRequest request);

    // دریافت اطلاعات کاربر
    Task<UserInfoResponse> GetUserInfoAsync(int userId);

    // دریافت اطلاعات کاربر با نام کاربری
    Task<UserInfoResponse> GetUserInfoByUsernameAsync(string username);

    // فراموشی رمز عبور
    Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request);

    // بازنشانی رمز عبور
    Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request);

    // تجدید توکن
    Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request);

    // فعال سازی 2FA
    Task<TwoFactorSetupResponse> EnableTwoFactorAsync(int userId, string method);

    // غیرفعال سازی 2FA
    Task<ResponseBase> DisableTwoFactorAsync(int userId);

    // دریافت وضعیت 2FA
    Task<TwoFactorStatusResponse> GetTwoFactorStatusAsync(int userId);

    // بروزرسانی پروفایل
    Task<UpdateProfileResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request);
    // بروزرسانی کاربر (ادمین)
    Task<UpdateUserResponse> UpdateUserAsync(int userId, UpdateUserRequest request);

    // حذف کاربر (ادمین)
    Task<ResponseBase> DeleteUserAsync(int userId);
}

// ============================================================
// پیاده سازی سرویس احراز هویت
// ============================================================
public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuthService> _logger;
    private readonly ITokenService _tokenService;
    private readonly IOTPService _otpService;
    private readonly AuthOptions _options;

    public AuthService(
        AppDbContext context,
        ILogger<AuthService> logger,
        ITokenService tokenService,
        IOTPService otpService,
        IOptions<AuthOptions> options)
    {
        _context = context;
        _logger = logger;
        _tokenService = tokenService;
        _otpService = otpService;
        _options = options.Value;
    }
    // Services/AuthService.cs

    public async Task<UpdateUserResponse> UpdateUserAsync(int userId, UpdateUserRequest request)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Error<UpdateUserResponse>("کاربر یافت نشد");

            // بروزرسانی ایمیل
            if (!string.IsNullOrEmpty(request.Email) && request.Email != user.EmailAddress)
            {
                if (await _context.Users.AnyAsync(u => u.EmailAddress == request.Email && u.UserId != userId))
                    return Error<UpdateUserResponse>("این ایمیل قبلاً توسط کاربر دیگری ثبت شده است");

                user.EmailAddress = request.Email;
                user.IsEmailConfirmed = false;
            }

            // بروزرسانی شماره موبایل
            if (!string.IsNullOrEmpty(request.PhoneNumber) && request.PhoneNumber != user.PhoneNumber)
            {
                if (await _context.Users.AnyAsync(u => u.PhoneNumber == request.PhoneNumber && u.UserId != userId))
                    return Error<UpdateUserResponse>("این شماره موبایل قبلاً توسط کاربر دیگری ثبت شده است");

                user.PhoneNumber = request.PhoneNumber;
                user.IsPhoneConfirmed = false;
            }

            // بروزرسانی نقش
            if (!string.IsNullOrEmpty(request.Role))
            {
                user.Role = request.Role;
            }

            // بروزرسانی وضعیت فعال بودن
            if (request.IsActive.HasValue)
            {
                user.IsActive = request.IsActive.Value;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("User updated by admin: {UserId}", userId);

            return new UpdateUserResponse
            {
                Success = true,
                Message = "کاربر با موفقیت بروزرسانی شد",
                UserId = user.UserId,
                IsUpdated = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update user error");
            return Error<UpdateUserResponse>("خطا در بروزرسانی کاربر");
        }
    }

    public async Task<ResponseBase> DeleteUserAsync(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Error<ResponseBase>("کاربر یافت نشد");

            // جلوگیری از حذف ادمین اصلی
            if (user.Role == "Admin" && user.Username == "admin")
                return Error<ResponseBase>("حذف کاربر ادمین اصلی امکان‌پذیر نیست");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User deleted by admin: {UserId}", userId);

            return new ResponseBase
            {
                Success = true,
                Message = "کاربر با موفقیت حذف شد"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete user error");
            return Error<ResponseBase>("خطا در حذف کاربر");
        }
    }
    // ============================================================
    // ثبت نام
    // ============================================================
    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // اعتبارسنجی نام کاربری
            if (string.IsNullOrEmpty(request.Username) || request.Username.Length < 3)
                return Error<RegisterResponse>("نام کاربری باید حداقل ۳ کاراکتر باشد");

            // اعتبارسنجی رمز عبور
            if (string.IsNullOrEmpty(request.Password) || request.Password.Length < _options.MinimumPasswordLength)
                return Error<RegisterResponse>($"رمز عبور باید حداقل {_options.MinimumPasswordLength} کاراکتر باشد");

            // بررسی یکتایی نام کاربری
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                return Error<RegisterResponse>("این نام کاربری قبلاً ثبت شده است");

            // بررسی یکتایی ایمیل
            if (!string.IsNullOrEmpty(request.Email) && await _context.Users.AnyAsync(u => u.EmailAddress == request.Email))
                return Error<RegisterResponse>("این ایمیل قبلاً ثبت شده است");

            // بررسی یکتایی شماره موبایل
            if (!string.IsNullOrEmpty(request.PhoneNumber) && await _context.Users.AnyAsync(u => u.PhoneNumber == request.PhoneNumber))
                return Error<RegisterResponse>("این شماره موبایل قبلاً ثبت شده است");

            // ساخت کاربر جدید
            var user = new User
            {
                Username = request.Username,
                EmailAddress = request.Email,
                PhoneNumber = request.PhoneNumber,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password, _options.BCryptWorkFactor),
                Role = "User",
                IsActive = true,
                IsEmailConfirmed = false,
                IsPhoneConfirmed = false,
                CreatedAt = DateTime.UtcNow
            };

            // تولید توکن ها
            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // ذخیره در دیتابیس
            if (_context.Database.IsInMemory())
            {
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_options.RefreshTokenExpiryDays);
                await _context.SaveChangesAsync();
            }
            else
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_options.RefreshTokenExpiryDays);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }

            _logger.LogInformation("User registered: {Username} (ID: {UserId})", user.Username, user.UserId);

            return new RegisterResponse
            {
                Success = true,
                Message = "ثبت نام با موفقیت انجام شد",
                UserId = user.UserId,
                Username = user.Username,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = _tokenService.GetAccessTokenExpirySeconds(),
                RequiresEmailConfirmation = !user.IsEmailConfirmed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Register error");
            return Error<RegisterResponse>("خطای سیستمی رخ داده است");
        }
    }

    // ============================================================
    // ورود مرحله اول - نام کاربری و رمز عبور
    // ============================================================
    public async Task<LoginStep1Response> LoginStep1Async(LoginStep1Request request)
    {
        try
        {
            // پیدا کردن کاربر با نام کاربری
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null)
                return Error<LoginStep1Response>("نام کاربری یا رمز عبور اشتباه است");

            // بررسی قفل بودن
            if (user.LockoutEndTime.HasValue && user.LockoutEndTime > DateTime.UtcNow)
                return Error<LoginStep1Response>($"حساب کاربری تا {user.LockoutEndTime.Value.ToLocalTime():HH:mm} قفل است");

            // بررسی رمز عبور
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                user.FailedLoginAttempts++;
                user.LastLoginAttempt = DateTime.UtcNow;

                if (user.FailedLoginAttempts >= _options.MaxFailedAttempts)
                {
                    user.LockoutEndTime = DateTime.UtcNow.AddMinutes(_options.LockoutMinutes);
                    user.FailedLoginAttempts = 0;
                    await _context.SaveChangesAsync();
                    return Error<LoginStep1Response>("حساب کاربری به مدت ۱۵ دقیقه قفل شد");
                }

                await _context.SaveChangesAsync();
                return Error<LoginStep1Response>("نام کاربری یا رمز عبور اشتباه است");
            }

            // بررسی فعال بودن
            if (!user.IsActive)
                return Error<LoginStep1Response>("حساب کاربری غیرفعال است");

            // بررسی 2FA
            var availableMethods = new List<TwoFactorMethodInfo>();
            var userInfo = new UserInfoFor2FA
            {
                UserId = user.UserId,
                Username = user.Username,
                HasEmail = user.IsEmailConfirmed && !string.IsNullOrEmpty(user.EmailAddress),
                HasPhone = user.IsPhoneConfirmed && !string.IsNullOrEmpty(user.PhoneNumber),
                HasTOTP = user.TwoFactorEnabled && user.TwoFactorMethod == "TOTP"
            };

            if (user.TwoFactorEnabled)
            {
                if (userInfo.HasEmail)
                    availableMethods.Add(new TwoFactorMethodInfo
                    {
                        Type = "Email",
                        Label = "ایمیل",
                        IsAvailable = true,
                        MaskedDestination = MaskEmail(user.EmailAddress!)
                    });

                if (userInfo.HasPhone)
                    availableMethods.Add(new TwoFactorMethodInfo
                    {
                        Type = "SMS",
                        Label = "پیامک",
                        IsAvailable = true,
                        MaskedDestination = MaskPhone(user.PhoneNumber!)
                    });

                if (userInfo.HasTOTP)
                    availableMethods.Add(new TwoFactorMethodInfo
                    {
                        Type = "TOTP",
                        Label = "Google Authenticator",
                        IsAvailable = true,
                        MaskedDestination = null
                    });
            }
            else
            {
                return Error<LoginStep1Response>("احراز هویت دو مرحله ای برای این کاربر فعال نیست");
            }

            // تولید توکن موقت
            var loginToken = GenerateLoginToken();
            user.LoginToken = loginToken;
            user.LoginTokenExpiry = DateTime.UtcNow.AddMinutes(_options.LoginTokenExpiryMinutes);
            user.FailedLoginAttempts = 0;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Login step 1 completed for user {Username}", user.Username);

            return new LoginStep1Response
            {
                Success = true,
                Message = "احراز هویت اولیه انجام شد. لطفاً روش دریافت کد را انتخاب کنید.",
                LoginToken = loginToken,
                ExpiresIn = _options.LoginTokenExpiryMinutes * 60,
                RequiresTwoFactor = user.TwoFactorEnabled,
                AvailableMethods = availableMethods,
                UserInfo = userInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login step 1 error");
            return Error<LoginStep1Response>("خطای سیستمی رخ داده است");
        }
    }

    // ============================================================
    // ورود مرحله دوم - انتخاب روش دریافت کد
    // ============================================================
    public async Task<LoginStep2Response> LoginStep2Async(LoginStep2Request request)
    {
        try
        {
            // پیدا کردن کاربر با توکن موقت
            var user = await _context.Users.FirstOrDefaultAsync(u => u.LoginToken == request.LoginToken);
            if (user == null || user.LoginTokenExpiry < DateTime.UtcNow)
                return Error<LoginStep2Response>("توکن نامعتبر یا منقضی شده است");

            // بررسی روش انتخاب شده
            string? maskedDestination = null;

            switch (request.Method)
            {
                case "Email":
                    if (string.IsNullOrEmpty(user.EmailAddress) || !user.IsEmailConfirmed)
                        return Error<LoginStep2Response>("ایمیل تایید شده ای برای این کاربر وجود ندارد");

                    maskedDestination = MaskEmail(user.EmailAddress);

                    // ارسال کد به ایمیل
                    await _otpService.SendOTPAsync(new SendOTPRequest
                    {
                        UserId = user.UserId,
                        Type = "Login",
                        Channel = "Email"
                    });
                    break;

                case "SMS":
                    if (string.IsNullOrEmpty(user.PhoneNumber) || !user.IsPhoneConfirmed)
                        return Error<LoginStep2Response>("شماره موبایل تایید شده ای برای این کاربر وجود ندارد");

                    maskedDestination = MaskPhone(user.PhoneNumber);

                    // ارسال کد به پیامک
                    await _otpService.SendOTPAsync(new SendOTPRequest
                    {
                        UserId = user.UserId,
                        Type = "Login",
                        Channel = "SMS"
                    });
                    break;

                case "TOTP":
                    if (!user.TwoFactorEnabled || user.TwoFactorMethod != "TOTP")
                        return Error<LoginStep2Response>("Google Authenticator برای این کاربر فعال نیست");
                    break;

                default:
                    return Error<LoginStep2Response>("روش انتخاب شده معتبر نیست");
            }

            _logger.LogInformation("OTP sent to {Username} via {Method}", user.Username, request.Method);

            return new LoginStep2Response
            {
                Success = true,
                Message = request.Method == "TOTP"
                    ? "لطفاً کد Google Authenticator خود را وارد کنید"
                    : "کد تایید با موفقیت ارسال شد",
                Method = request.Method,
                ExpiresIn = _options.OTPExpiryMinutes * 60,
                MaskedDestination = maskedDestination
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login step 2 error");
            return Error<LoginStep2Response>("خطا در ارسال کد تایید");
        }
    }

    // ============================================================
    // ورود مرحله سوم - تایید کد
    // ============================================================
    public async Task<LoginStep3Response> LoginStep3Async(LoginStep3Request request)
    {
        try
        {
            // پیدا کردن کاربر با توکن موقت
            var user = await _context.Users.FirstOrDefaultAsync(u => u.LoginToken == request.LoginToken);
            if (user == null || user.LoginTokenExpiry < DateTime.UtcNow)
                return Error<LoginStep3Response>("توکن نامعتبر یا منقضی شده است");

            // تایید کد
            if (user.TwoFactorMethod == "TOTP")
            {
                // تایید TOTP
                var isValid = await _otpService.VerifyTOTPAsync(user.UserId, request.Code);
                if (!isValid)
                    return Error<LoginStep3Response>("کد Google Authenticator نامعتبر است");
            }
            else
            {
                // تایید OTP
                var verifyResult = await _otpService.VerifyOTPAsync(new VerifyOTPRequest
                {
                    UserId = user.UserId,
                    Type = "Login",
                    Code = request.Code
                });

                if (!verifyResult.Success)
                    return Error<LoginStep3Response>(verifyResult.Message ?? "کد نامعتبر است");
            }

            // پاک کردن توکن موقت
            user.LoginToken = null;
            user.LoginTokenExpiry = null;
            user.LastLoginAt = DateTime.UtcNow;
            user.LastLoginIp = request.IpAddress;

            // تولید توکن های جدید
            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_options.RefreshTokenExpiryDays);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User logged in: {Username}", user.Username);

            return new LoginStep3Response
            {
                Success = true,
                Message = "ورود با موفقیت انجام شد",
                UserId = user.UserId,
                Username = user.Username,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = _tokenService.GetAccessTokenExpirySeconds()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login step 3 error");
            return Error<LoginStep3Response>("خطا در تایید کد");
        }
    }

    // ============================================================
    // خروج
    // ============================================================
    public async Task<LogoutResponse> LogoutAsync(LogoutRequest request)
    {
        try
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("User logged out: {UserId}", request.UserId);

            return new LogoutResponse
            {
                Success = true,
                Message = "خروج با موفقیت انجام شد",
                IsLoggedOut = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout error");
            return Error<LogoutResponse>("خطا در خروج از سیستم");
        }
    }

    // ============================================================
    // تغییر رمز عبور
    // ============================================================
    public async Task<ChangePasswordResponse> ChangePasswordAsync(ChangePasswordRequest request)
    {
        try
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
                return Error<ChangePasswordResponse>("کاربر یافت نشد");

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
                return Error<ChangePasswordResponse>("رمز فعلی اشتباه است");

            if (request.NewPassword.Length < _options.MinimumPasswordLength)
                return Error<ChangePasswordResponse>($"رمز باید حداقل {_options.MinimumPasswordLength} کاراکتر باشد");

            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, _options.BCryptWorkFactor);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Password changed: {UserId}", user.UserId);

            return new ChangePasswordResponse
            {
                Success = true,
                Message = "رمز عبور با موفقیت تغییر کرد",
                IsChanged = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change password error");
            return Error<ChangePasswordResponse>("خطا در تغییر رمز");
        }
    }

    // ============================================================
    // دریافت اطلاعات کاربر
    // ============================================================
    public async Task<UserInfoResponse> GetUserInfoAsync(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Error<UserInfoResponse>("کاربر یافت نشد");

            return new UserInfoResponse
            {
                Success = true,
                Message = "اطلاعات کاربر دریافت شد",
                UserId = user.UserId,
                Username = user.Username,
                Email = user.EmailAddress,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                IsActive = user.IsActive,
                IsEmailConfirmed = user.IsEmailConfirmed,
                IsPhoneConfirmed = user.IsPhoneConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get user info error");
            return Error<UserInfoResponse>("خطا در دریافت اطلاعات");
        }
    }

    // ============================================================
    // دریافت اطلاعات کاربر با نام کاربری
    // ============================================================
    public async Task<UserInfoResponse> GetUserInfoByUsernameAsync(string username)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return Error<UserInfoResponse>("کاربر یافت نشد");

            return new UserInfoResponse
            {
                Success = true,
                Message = "اطلاعات کاربر دریافت شد",
                UserId = user.UserId,
                Username = user.Username,
                Email = user.EmailAddress,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                IsActive = user.IsActive,
                IsEmailConfirmed = user.IsEmailConfirmed,
                IsPhoneConfirmed = user.IsPhoneConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get user info by username error");
            return Error<UserInfoResponse>("خطا در دریافت اطلاعات");
        }
    }

    // ============================================================
    // فراموشی رمز عبور
    // ============================================================
    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        try
        {
            var user = await FindUserByEmailOrPhoneAsync(request.Email, request.PhoneNumber);
            if (user == null)
                return Error<ForgotPasswordResponse>("کاربری با این مشخصات یافت نشد");

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            user.ResetPasswordToken = token;
            user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(_options.ResetTokenExpiryHours);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Reset token generated for user: {Username}", user.Username);

            return new ForgotPasswordResponse
            {
                Success = true,
                Message = "لینک بازنشانی به ایمیل شما ارسال شد",
                IsSent = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Forgot password error");
            return Error<ForgotPasswordResponse>("خطا در ارسال لینک بازنشانی");
        }
    }

    // ============================================================
    // بازنشانی رمز عبور
    // ============================================================
    public async Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        try
        {
            var user = await FindUserByEmailOrPhoneAsync(request.Email, request.PhoneNumber);
            if (user == null)
                return Error<ResetPasswordResponse>("کاربر یافت نشد");

            if (user.ResetPasswordToken != request.Token)
                return Error<ResetPasswordResponse>("توکن نامعتبر است");

            if (user.ResetPasswordTokenExpiry < DateTime.UtcNow)
                return Error<ResetPasswordResponse>("توکن منقضی شده است");

            if (request.NewPassword.Length < _options.MinimumPasswordLength)
                return Error<ResetPasswordResponse>($"رمز باید حداقل {_options.MinimumPasswordLength} کاراکتر باشد");

            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, _options.BCryptWorkFactor);
            user.ResetPasswordToken = null;
            user.ResetPasswordTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Password reset for user: {Username}", user.Username);

            return new ResetPasswordResponse
            {
                Success = true,
                Message = "رمز عبور با موفقیت بازنشانی شد",
                IsReset = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reset password error");
            return Error<ResetPasswordResponse>("خطا در بازنشانی رمز");
        }
    }

    // ============================================================
    // تجدید توکن
    // ============================================================
    public async Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);
            if (user == null)
                return Error<RefreshTokenResponse>("توکن نامعتبر است");

            if (user.RefreshTokenExpiryTime < DateTime.UtcNow)
                return Error<RefreshTokenResponse>("توکن منقضی شده است");

            var newAccessToken = _tokenService.GenerateAccessToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_options.RefreshTokenExpiryDays);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Token refreshed for user: {Username}", user.Username);

            return new RefreshTokenResponse
            {
                Success = true,
                Message = "توکن با موفقیت تجدید شد",
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = _tokenService.GetAccessTokenExpirySeconds()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh token error");
            return Error<RefreshTokenResponse>("خطا در تجدید توکن");
        }
    }

    // ============================================================
    // فعال سازی 2FA
    // ============================================================
    public async Task<TwoFactorSetupResponse> EnableTwoFactorAsync(int userId, string method)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Error<TwoFactorSetupResponse>("کاربر یافت نشد");

            if (method == "TOTP")
            {
                var setupResult = await _otpService.SetupTOTPAsync(userId);

                // ذخیره اطلاعات
                user.TwoFactorSecret = setupResult.SecretKey;
                user.TwoFactorMethod = "TOTP";
                user.TwoFactorEnabled = true;
                await _context.SaveChangesAsync();

                return new TwoFactorSetupResponse
                {
                    Success = true,
                    Message = "تنظیمات TOTP انجام شد",
                    SecretKey = setupResult.SecretKey,
                    QRCodeUrl = setupResult.QRCodeUrl,
                    BackupCodes = setupResult.BackupCodes
                };
            }

            return Error<TwoFactorSetupResponse>("روش پشتیبانی نمی شود");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Enable 2FA error");
            return Error<TwoFactorSetupResponse>("خطا در فعال سازی");
        }
    }

    // ============================================================
    // غیرفعال سازی 2FA
    // ============================================================
    public async Task<ResponseBase> DisableTwoFactorAsync(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Error<ResponseBase>("کاربر یافت نشد");

            user.TwoFactorEnabled = false;
            user.TwoFactorSecret = null;
            user.TwoFactorMethod = null;
            await _context.SaveChangesAsync();

            _logger.LogInformation("2FA disabled for user: {UserId}", userId);

            return new ResponseBase
            {
                Success = true,
                Message = "احراز هویت دو مرحله ای غیرفعال شد"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Disable 2FA error");
            return Error<ResponseBase>("خطا در غیرفعال سازی");
        }
    }

    // ============================================================
    // دریافت وضعیت 2FA
    // ============================================================
    public async Task<TwoFactorStatusResponse> GetTwoFactorStatusAsync(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Error<TwoFactorStatusResponse>("کاربر یافت نشد");

            return new TwoFactorStatusResponse
            {
                Success = true,
                Message = "وضعیت دریافت شد",
                IsEnabled = user.TwoFactorEnabled,
                Method = user.TwoFactorMethod ?? "None"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get 2FA status error");
            return Error<TwoFactorStatusResponse>("خطا در دریافت وضعیت");
        }
    }

    // ============================================================
    // بروزرسانی پروفایل
    // ============================================================
    public async Task<UpdateProfileResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Error<UpdateProfileResponse>("کاربر یافت نشد");

            // بروزرسانی ایمیل
            if (!string.IsNullOrEmpty(request.Email) && request.Email != user.EmailAddress)
            {
                if (await _context.Users.AnyAsync(u => u.EmailAddress == request.Email && u.UserId != userId))
                    return Error<UpdateProfileResponse>("این ایمیل قبلاً توسط کاربر دیگری ثبت شده است");

                user.EmailAddress = request.Email;
                user.IsEmailConfirmed = false;
            }

            // بروزرسانی شماره موبایل
            if (!string.IsNullOrEmpty(request.PhoneNumber) && request.PhoneNumber != user.PhoneNumber)
            {
                if (await _context.Users.AnyAsync(u => u.PhoneNumber == request.PhoneNumber && u.UserId != userId))
                    return Error<UpdateProfileResponse>("این شماره موبایل قبلاً توسط کاربر دیگری ثبت شده است");

                user.PhoneNumber = request.PhoneNumber;
                user.IsPhoneConfirmed = false;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Profile updated for user: {UserId}", userId);

            return new UpdateProfileResponse
            {
                Success = true,
                Message = "پروفایل با موفقیت بروزرسانی شد",
                IsUpdated = true,
                Email = user.EmailAddress,
                PhoneNumber = user.PhoneNumber,
                IsEmailConfirmed = user.IsEmailConfirmed,
                IsPhoneConfirmed = user.IsPhoneConfirmed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update profile error");
            return Error<UpdateProfileResponse>("خطا در بروزرسانی پروفایل");
        }

    }

    // ============================================================
    // متدهای کمکی
    // ============================================================

    private async Task<User?> FindUserByEmailOrPhoneAsync(string? email, string? phone)
    {
        if (!string.IsNullOrEmpty(email))
            return await _context.Users.FirstOrDefaultAsync(u => u.EmailAddress == email);

        if (!string.IsNullOrEmpty(phone))
            return await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);

        return null;
    }

    private string GenerateLoginToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private string MaskEmail(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2) return email;

        var name = parts[0];
        var domain = parts[1];

        if (name.Length <= 2) return $"{name}***@{domain}";
        return $"{name.Substring(0, 2)}***@{domain}";
    }

    private string MaskPhone(string phone)
    {
        if (string.IsNullOrEmpty(phone) || phone.Length < 6) return phone;
        return $"{phone.Substring(0, 3)}***{phone.Substring(phone.Length - 3)}";
    }

    private T Error<T>(string message) where T : ResponseBase, new()
    {
        return new T { Success = false, Message = message };
    }
}