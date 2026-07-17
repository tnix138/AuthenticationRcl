using AuthenticationRcl.Models;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AuthenticationRcl.Data;

/// <summary>
/// کلاس برای Seed کردن داده‌های اولیه دیتابیس
/// </summary>
public static class SeedData
{
    /// <summary>
    /// مقداردهی اولیه دیتابیس با داده‌های پیش‌فرض
    /// </summary>
    /// <param name="serviceProvider">سرویس‌های برنامه</param>
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            // اعمال Migration
            await context.Database.MigrateAsync();

            // ایجاد کاربر ادمین اگر وجود نداشت
            if (!await context.Users.AnyAsync(u => u.Role == "Admin"))
            {
                var adminUser = new User
                {
                    EmailAddress = "admin@example.com",
                    PhoneNumber = "09123456789",
                    Password = BCrypt.Net.BCrypt.HashPassword("Admin@123456"),
                    Role = "Admin",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await context.Users.AddAsync(adminUser);
                await context.SaveChangesAsync();

                logger.LogInformation("کاربر ادمین با موفقیت ایجاد شد");
            }

            // ایجاد کاربر تست اگر وجود نداشت
            if (!await context.Users.AnyAsync(u => u.EmailAddress == "test@example.com"))
            {
                var testUser = new User
                {
                    EmailAddress = "test@example.com",
                    PhoneNumber = "09123456788",
                    Password = BCrypt.Net.BCrypt.HashPassword("Test@123456"),
                    Role = "User",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await context.Users.AddAsync(testUser);
                await context.SaveChangesAsync();

                logger.LogInformation("کاربر تست با موفقیت ایجاد شد");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در Seed کردن دیتابیس");
        }
    }
}