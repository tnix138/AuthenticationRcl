namespace AuthenticationRcl.Services;

public interface ISMSProvider
{
    Task<bool> SendOTPAsync(string phoneNumber, string code);                 // ارسال کد تایید به پیامک
    Task<bool> SendResetPasswordLinkAsync(string phoneNumber, string token);  // ارسال لینک بازنشانی رمز
}