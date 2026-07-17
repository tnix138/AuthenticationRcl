namespace AuthenticationRcl.ViewModels;

/// <summary>
/// کلاس پایه برای تمام پاسخ‌های API
/// </summary>
/// <remarks>
/// تمام کلاس‌های Response از این کلاس ارث‌بری می‌کنند
/// این کلاس ساختار یکپارچه‌ای برای پاسخ‌های API فراهم می‌کند
/// </remarks>
public class ResponseBase
{
    /// <summary>
    /// وضعیت موفقیت‌آمیز بودن عملیات
    /// </summary>
    /// <remarks>
    /// true: عملیات با موفقیت انجام شده
    /// false: عملیات با خطا مواجه شده
    /// </remarks>
    public bool IsSucceded { get; set; }

    /// <summary>
    /// پیام خطا برای توسعه‌دهنده
    /// </summary>
    /// <remarks>
    /// شامل جزئیات فنی خطا برای دیباگ کردن
    /// در محیط Production می‌تواند نمایش داده نشود
    /// </remarks>
    public string? DevMessage { get; set; }

    /// <summary>
    /// پیام نمایشی برای کاربر
    /// </summary>
    /// <remarks>
    /// پیامی کاربرپسند و قابل فهم برای نمایش به کاربر نهایی
    /// به زبان فارسی است
    /// </remarks>
    public string? Message { get; set; }

    /// <summary>
    /// آدرس هدایت پس از عملیات موفق
    /// </summary>
    /// <remarks>
    /// در صورت وجود، کاربر پس از عملیات به این آدرس هدایت می‌شود
    /// معمولاً برای هدایت به صفحه بعدی استفاده می‌شود
    /// </remarks>
    public string? RedirectUrl { get; set; }
}