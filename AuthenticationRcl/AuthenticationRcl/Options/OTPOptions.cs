namespace AuthenticationRcl.Options;

public class OTPOptions
{
    public int Length { get; set; } = 6;                // طول کد تایید
    public int ExpiryMinutes { get; set; } = 5;         // مدت اعتبار کد تایید
    public int MaxAttempts { get; set; } = 3;           // حداکثر تلاش برای تایید
}