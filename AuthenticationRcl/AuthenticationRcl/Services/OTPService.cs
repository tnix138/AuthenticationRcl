using System.Security.Cryptography;
using System.Text;
using AuthenticationRcl.Models;
using AuthenticationRcl.Options;
using AuthenticationRcl.ViewModels.Base;
using AuthenticationRcl.ViewModels.OTP;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthenticationRcl.Services;

public class OTPService : IOTPService
{
    private readonly AppDbContext _context;
    private readonly ILogger<OTPService> _logger;
    private readonly IEmailProvider _emailProvider;
    private readonly ISMSProvider _smsProvider;
    private readonly OTPOptions _options;

    public OTPService(
        AppDbContext context,
        ILogger<OTPService> logger,
        IEmailProvider emailProvider,
        ISMSProvider smsProvider,
        IOptions<OTPOptions> options)
    {
        _context = context;
        _logger = logger;
        _emailProvider = emailProvider;
        _smsProvider = smsProvider;
        _options = options.Value;
    }

    public async Task<SendOTPResponse> SendOTPAsync(SendOTPRequest request)
    {
        try
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
                return Error<SendOTPResponse>("کاربر یافت نشد");

            var code = GenerateOTPCode(_options.Length);
            var hashedCode = HashOTPCode(code);

            var otp = new UserOTP
            {
                UserId = request.UserId,
                Type = request.Type,
                Channel = request.Channel,
                Code = hashedCode,
                ExpiryTime = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes),
                CreatedAt = DateTime.UtcNow
            };

            await _context.UserOTPs.AddAsync(otp);
            await _context.SaveChangesAsync();

            var sent = await SendOTPByChannelAsync(user, code, request.Channel);

            if (!sent)
                return Error<SendOTPResponse>("خطا در ارسال کد");

            _logger.LogInformation("OTP sent to {UserId} via {Channel}", user.UserId, request.Channel);

            return new SendOTPResponse
            {
                Success = true,
                Message = "کد تایید ارسال شد",
                OtpId = otp.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Send OTP error");
            return Error<SendOTPResponse>("خطا در ارسال کد");
        }
    }

    public async Task<VerifyOTPResponse> VerifyOTPAsync(VerifyOTPRequest request)
    {
        try
        {
            var otp = await _context.UserOTPs
                .Where(o => o.UserId == request.UserId
                    && o.Type == request.Type
                    && !o.IsUsed
                    && o.ExpiryTime > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otp == null)
                return Error<VerifyOTPResponse>("کد نامعتبر یا منقضی شده است");

            if (otp.AttemptCount >= _options.MaxAttempts)
            {
                otp.IsUsed = true;
                await _context.SaveChangesAsync();
                return Error<VerifyOTPResponse>("تعداد تلاش بیش از حد مجاز");
            }

            if (!VerifyOTPCode(request.Code, otp.Code))
            {
                otp.AttemptCount++;
                await _context.SaveChangesAsync();
                return Error<VerifyOTPResponse>("کد نامعتبر است");
            }

            otp.IsUsed = true;
            otp.UsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("OTP verified for {UserId}", request.UserId);

            return new VerifyOTPResponse
            {
                Success = true,
                Message = "کد با موفقیت تایید شد",
                IsVerified = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Verify OTP error");
            return Error<VerifyOTPResponse>("خطا در تایید کد");
        }
    }
    // Services/OTPService.cs

    public async Task<TOTPSetupResult> SetupTOTPAsync(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return new TOTPSetupResult { IsSuccess = false, Message = "کاربر یافت نشد" };

            // تولید کلید مخفی TOTP
            var secretKey = OtpNet.Base32Encoding.ToString(OtpNet.KeyGeneration.GenerateRandomKey(20));

            // تولید QR Code URL
            var issuer = "AuthenticationRcl";
            var accountName = user.Username;
            var qrCodeUrl = $"otpauth://totp/{issuer}:{accountName}?secret={secretKey}&issuer={issuer}";

            // ذخیره کلید در دیتابیس (فعلاً غیرفعال)
            user.TwoFactorSecret = secretKey;
            user.TwoFactorMethod = "TOTP";
            await _context.SaveChangesAsync();

            // تولید کدهای پشتیبان
            var backupCodes = await GenerateBackupCodesAsync(userId, 10);

            return new TOTPSetupResult
            {
                IsSuccess = true,
                Message = "تنظیمات TOTP با موفقیت انجام شد",
                SecretKey = secretKey,
                QRCodeUrl = qrCodeUrl,
                BackupCodes = backupCodes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Setup TOTP error for user {UserId}", userId);
            return new TOTPSetupResult { IsSuccess = false, Message = "خطا در تنظیم TOTP" };
        }
    }
    public async Task<bool> VerifyTOTPAsync(int userId, string code)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.TwoFactorSecret))
                return false;

            var secretBytes = OtpNet.Base32Encoding.ToBytes(user.TwoFactorSecret);

            var totp = new OtpNet.Totp(secretBytes);
            var isValid = totp.VerifyTotp(code, out _);

            if (isValid)
            {
                user.TwoFactorEnabled = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation("TOTP verified and enabled for user {UserId}", userId);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Verify TOTP error for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> DisableTOTPAsync(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.TwoFactorEnabled = false;
            user.TwoFactorSecret = null;
            user.TwoFactorMethod = null;
            await _context.SaveChangesAsync();

            _logger.LogInformation("TOTP disabled for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Disable TOTP error for user {UserId}", userId);
            return false;
        }
    }
    public Task<List<string>> GenerateBackupCodesAsync(int userId, int count)
    {
        var codes = new List<string>();
        for (int i = 0; i < count; i++)
        {
            codes.Add(GenerateOTPCode(8));
        }
        return Task.FromResult(codes);
    }

    private string GenerateOTPCode(int length)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        return string.Concat(bytes.Select(b => (b % 10).ToString()));
    }

    private string HashOTPCode(string code)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(code));
        return Convert.ToBase64String(bytes);
    }

    private bool VerifyOTPCode(string code, string hashedCode)
    {
        return HashOTPCode(code) == hashedCode;
    }

    private async Task<bool> SendOTPByChannelAsync(User user, string code, string channel)
    {
        try
        {
            if (channel == "Email" && !string.IsNullOrEmpty(user.EmailAddress))
            {
                var result = await _emailProvider.SendOTPAsync(user.EmailAddress, code);
                return result;
            }

            if (channel == "SMS" && !string.IsNullOrEmpty(user.PhoneNumber))
            {
                var result = await _smsProvider.SendOTPAsync(user.PhoneNumber, code);
                return result;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Send OTP by channel error");
            return false;
        }
    }

    private T Error<T>(string message) where T : ResponseBase, new()
    {
        return new T { Success = false, Message = message };
    }
}