using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthenticationRcl.Models;

public class UserBackupCode
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }                           // شناسه یکتا

    public int UserId { get; set; }                       // شناسه کاربر

    public User User { get; set; } = null!;               // کاربر مرتبط

    [MaxLength(255)]
    public string Code { get; set; } = string.Empty;      // کد پشتیبان هش شده

    public bool IsUsed { get; set; } = false;             // وضعیت استفاده شده

    public DateTime? UsedAt { get; set; }                 // زمان استفاده

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // تاریخ ایجاد
}