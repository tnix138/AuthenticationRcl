using AuthenticationRcl.ViewModels.Base;

namespace AuthenticationRcl.ViewModels.OTP;

public class SendOTPRequest
{
    public int UserId { get; set; }                 // شناسه کاربر
    public string Type { get; set; } = "TwoFactor"; // نوع OTP (Login, Register, TwoFactor)
    public string Channel { get; set; } = "Email";  // کانال ارسال (Email, SMS)
}

public class SendOTPResponse : ResponseBase
{
    public int OtpId { get; set; }                  // شناسه OTP ذخیره شده
}

public class VerifyOTPRequest
{
    public int UserId { get; set; }                 // شناسه کاربر
    public string Type { get; set; } = "TwoFactor"; // نوع OTP
    public string Code { get; set; } = string.Empty; // کد تایید
}

public class VerifyOTPResponse : ResponseBase
{
    public bool IsVerified { get; set; }            // وضعیت تایید
}