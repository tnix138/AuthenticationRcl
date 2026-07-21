namespace AuthenticationRcl.ViewModels.Base;

public class ResponseBase
{
    public bool Success { get; set; }           // وضعیت موفقیت
    public string? Message { get; set; }        // پیام نمایشی به کاربر
    public string? DevMessage { get; set; }     // پیام فنی برای توسعه دهنده
}