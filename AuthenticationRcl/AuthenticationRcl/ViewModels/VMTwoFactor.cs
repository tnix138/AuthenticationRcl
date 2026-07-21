using AuthenticationRcl.ViewModels.Base;

namespace AuthenticationRcl.ViewModels.TwoFactor;

public class TwoFactorSetupResponse : ResponseBase
{
    public string? SecretKey { get; set; }              // کلید مخفی TOTP
    public string? QRCodeUrl { get; set; }              // آدرس QR Code
    public List<string>? BackupCodes { get; set; }      // کدهای پشتیبان
}

public class TwoFactorStatusResponse : ResponseBase
{
    public bool IsEnabled { get; set; }                 // وضعیت فعال بودن 2FA
    public string Method { get; set; } = "None";        // روش 2FA (Email, SMS, TOTP)
}

public class VerifyTOTPRequest
{
    public string Code { get; set; } = string.Empty;    // کد TOTP
}