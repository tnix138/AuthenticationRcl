using AuthenticationRcl.ViewModels.OTP;

namespace AuthenticationRcl.Services;

public interface IOTPService
{
    Task<SendOTPResponse> SendOTPAsync(SendOTPRequest request);           // ارسال کد تایید
    Task<VerifyOTPResponse> VerifyOTPAsync(VerifyOTPRequest request);     // تایید کد
    Task<TOTPSetupResult> SetupTOTPAsync(int userId);                     // تنظیم TOTP
    Task<bool> VerifyTOTPAsync(int userId, string code);                  // تایید TOTP
    Task<bool> DisableTOTPAsync(int userId);                              // غیرفعال سازی TOTP
    Task<List<string>> GenerateBackupCodesAsync(int userId, int count);   // تولید کدهای پشتیبان
}

public class TOTPSetupResult
{
    public bool IsSuccess { get; set; }          // وضعیت موفقیت
    public string? Message { get; set; }         // پیام
    public string? SecretKey { get; set; }       // کلید مخفی
    public string? QRCodeUrl { get; set; }       // آدرس QR Code
    public List<string>? BackupCodes { get; set; } // کدهای پشتیبان
}