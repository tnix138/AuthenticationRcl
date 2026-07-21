using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthenticationRcl.Models;

public class UserOTP
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }                           // شناسه یکتا

    public int UserId { get; set; }                       // شناسه کاربر

    public User User { get; set; } = null!;               // کاربر مرتبط

    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;      // نوع OTP (Login, Register, TwoFactor)

    [MaxLength(50)]
    public string Channel { get; set; } = string.Empty;   // کانال ارسال (Email, SMS)

    [MaxLength(255)]
    public string Code { get; set; } = string.Empty;      // کد هش شده

    public DateTime ExpiryTime { get; set; }              // زمان انقضا

    public bool IsUsed { get; set; } = false;             // وضعیت استفاده شده

    public int AttemptCount { get; set; } = 0;            // تعداد تلاش های ناموفق

    public DateTime? UsedAt { get; set; }                 // زمان استفاده

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // تاریخ ایجاد
}