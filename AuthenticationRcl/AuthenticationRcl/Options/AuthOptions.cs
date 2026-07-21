namespace AuthenticationRcl.Options;

public class AuthOptions
{
    public int MinimumPasswordLength { get; set; } = 8;          // حداقل طول رمز عبور
    public int MaxFailedAttempts { get; set; } = 5;              // حداکثر تلاش ناموفق
    public int LockoutMinutes { get; set; } = 15;                // مدت زمان قفل حساب
    public int RefreshTokenExpiryDays { get; set; } = 30;        // مدت اعتبار توکن بازنشانی
    public int ResetTokenExpiryHours { get; set; } = 1;          // مدت اعتبار توکن بازنشانی رمز
    public int LoginTokenExpiryMinutes { get; set; } = 5;        // مدت اعتبار توکن موقت ورود
    public int OTPExpiryMinutes { get; set; } = 5;               // مدت اعتبار کد تایید
    public int BCryptWorkFactor { get; set; } = 12;              // سطح هزینه BCrypt
}