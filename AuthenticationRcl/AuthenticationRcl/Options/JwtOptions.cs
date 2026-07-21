namespace AuthenticationRcl.Options;

public class JwtOptions
{
    public string SecretKey { get; set; } = string.Empty;              // کلید مخفی امضای توکن
    public string Issuer { get; set; } = string.Empty;                 // صادرکننده توکن
    public string Audience { get; set; } = string.Empty;               // مخاطب توکن
    public int AccessTokenExpirySeconds { get; set; } = 900;           // مدت اعتبار توکن دسترسی
}