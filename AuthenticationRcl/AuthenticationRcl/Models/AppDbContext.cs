using Microsoft.EntityFrameworkCore;

namespace AuthenticationRcl.Models;

/// <summary>
/// کلاس Context اصلی برای اتصال به دیتابیس
/// </summary>
public class AppDbContext : DbContext
{
    #region سازنده

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    #endregion

    #region DbSet

    public DbSet<User> Users { get; set; }

    #endregion

    #region پیکربندی مدل

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            // کلید اصلی
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).UseIdentityColumn(1, 1);

            // فیلدهای اصلی
            entity.Property(e => e.EmailAddress).HasMaxLength(256);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Password).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(50).HasDefaultValue("User");

            // فیلدهای امنیتی
            entity.Property(e => e.FailedLoginAttempts).HasDefaultValue(0);
            entity.Property(e => e.LockoutEndTime).IsRequired(false);
            entity.Property(e => e.LastLoginAttempt).IsRequired(false);

            // فیلدهای Refresh Token
            entity.Property(e => e.RefreshToken).HasMaxLength(255).IsRequired(false);
            entity.Property(e => e.RefreshTokenExpiryTime).IsRequired(false);

            // فیلدهای فراموشی رمز
            entity.Property(e => e.ResetPasswordToken).HasMaxLength(255).IsRequired(false);
            entity.Property(e => e.ResetPasswordTokenExpiry).IsRequired(false);

            // فیلدهای تاریخی
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).IsRequired(false);
            entity.Property(e => e.LastLoginAt).IsRequired(false);
            entity.Property(e => e.LastLoginIp).HasMaxLength(50).IsRequired(false);

            // ایندکس‌ها
            entity.HasIndex(e => e.EmailAddress).IsUnique()
                .HasDatabaseName("IX_Users_EmailAddress");
            entity.HasIndex(e => e.PhoneNumber).IsUnique()
                .HasDatabaseName("IX_Users_PhoneNumber");
            entity.HasIndex(e => e.ResetPasswordToken)
                .HasDatabaseName("IX_Users_ResetPasswordToken");
        });
    }

    #endregion
}