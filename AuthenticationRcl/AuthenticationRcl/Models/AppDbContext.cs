using Microsoft.EntityFrameworkCore;

namespace AuthenticationRcl.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<UserOTP> UserOTPs { get; set; }
    public DbSet<UserDevice> UserDevices { get; set; }
    public DbSet<UserBackupCode> UserBackupCodes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).UseIdentityColumn();

            // ایندکس یکتا برای Username
            entity.HasIndex(e => e.Username).IsUnique().HasDatabaseName("IX_Users_Username");

            // ایندکس یکتا برای Email (اگر خالی نباشد)
            entity.HasIndex(e => e.EmailAddress).IsUnique().HasDatabaseName("IX_Users_Email").HasFilter("[EmailAddress] IS NOT NULL");

            // ایندکس یکتا برای PhoneNumber (اگر خالی نباشد)
            entity.HasIndex(e => e.PhoneNumber).IsUnique().HasDatabaseName("IX_Users_Phone").HasFilter("[PhoneNumber] IS NOT NULL");

            entity.Property(e => e.Password).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Role).HasMaxLength(50).HasDefaultValue("User");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasMany(e => e.OTPs)
                  .WithOne(e => e.User)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserOTP>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();

            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_UserOTPs_UserId");
            entity.HasIndex(e => e.Code).HasDatabaseName("IX_UserOTPs_Code");
        });

        modelBuilder.Entity<UserDevice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();

            entity.HasIndex(e => new { e.UserId, e.DeviceId }).IsUnique()
                  .HasDatabaseName("IX_UserDevices_UserId_DeviceId");
        });

        modelBuilder.Entity<UserBackupCode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityColumn();

            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_UserBackupCodes_UserId");
        });

        // Seed Data
        var adminPassword = BCrypt.Net.BCrypt.HashPassword("Admin@123");
        var testPassword = BCrypt.Net.BCrypt.HashPassword("Test@123");

        modelBuilder.Entity<User>().HasData(
            new User
            {
                UserId = 1,
                Username = "admin",
                EmailAddress = "admin@example.com",
                PhoneNumber = "09123456789",
                Password = adminPassword,
                Role = "Admin",
                IsActive = true,
                IsEmailConfirmed = true,
                IsPhoneConfirmed = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                UserId = 2,
                Username = "test",
                EmailAddress = "test@example.com",
                PhoneNumber = "09123456788",
                Password = testPassword,
                Role = "User",
                IsActive = true,
                IsEmailConfirmed = true,
                IsPhoneConfirmed = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}