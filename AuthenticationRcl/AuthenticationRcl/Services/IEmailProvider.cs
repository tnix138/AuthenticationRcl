namespace AuthenticationRcl.Services;

public interface IEmailProvider
{
    Task<bool> SendOTPAsync(string email, string code);                 // ارسال کد تایید به ایمیل
    Task<bool> SendResetPasswordLinkAsync(string email, string token);  // ارسال لینک بازنشانی رمز
}