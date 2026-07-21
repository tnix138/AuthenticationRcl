using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthenticationRcl.Models;

public class UserDevice
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }                           // شناسه یکتا

    public int UserId { get; set; }                       // شناسه کاربر

    public User User { get; set; } = null!;               // کاربر مرتبط

    [MaxLength(255)]
    public string DeviceId { get; set; } = string.Empty;  // شناسه دستگاه

    [MaxLength(100)]
    public string DeviceName { get; set; } = string.Empty; // نام دستگاه

    [MaxLength(50)]
    public string DeviceType { get; set; } = string.Empty; // نوع دستگاه

    [MaxLength(50)]
    public string? OS { get; set; }                       // سیستم عامل

    [MaxLength(50)]
    public string? Browser { get; set; }                  // مرورگر

    [MaxLength(50)]
    public string? IPAddress { get; set; }                // آی پی دستگاه

    public string? UserAgent { get; set; }                // UserAgent

    public bool IsTrusted { get; set; } = false;          // دستگاه مورد اعتماد

    public bool IsActive { get; set; } = true;            // وضعیت فعال

    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow; // آخرین استفاده

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // تاریخ ایجاد
}