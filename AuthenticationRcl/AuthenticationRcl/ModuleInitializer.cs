using System.Text;
using AuthenticationRcl.Models;
using AuthenticationRcl.Options;
using AuthenticationRcl.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;

namespace AuthenticationRcl;

public static class ModuleInitializer
{
    public static IServiceCollection AddAuthenticationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ثبت تنظیمات
        services.Configure<AuthOptions>(configuration.GetSection("Auth"));
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<OTPOptions>(configuration.GetSection("OTP"));

        // ثبت DbContext
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("AuthDb")));

        // ثبت سرویس‌ها
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IOTPService, OTPService>();
        services.AddScoped<IAuthService, AuthService>();

        // ثبت Providers (پیاده‌سازی پیش‌فرض) - فقط یکبار
        services.AddScoped<IEmailProvider, DefaultEmailProvider>();
        services.AddScoped<ISMSProvider, DefaultSMSProvider>();

        // تنظیمات JWT
        var jwtSettings = configuration.GetSection("Jwt");
        var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? "DefaultSecretKey1234567890");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"] ?? "https://localhost:7001",
                    ValidAudience = jwtSettings["Audience"] ?? "https://localhost:7001",
                    IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                    ClockSkew = TimeSpan.FromSeconds(5)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/api/v1/chat"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        // محدودیت نرخ درخواست (Rate Limiting)
        services.AddRateLimiter(options =>
        {
            // سیاست کلی
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // سیاست ورود
            options.AddPolicy("LoginPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            // سیاست ثبت نام
            options.AddPolicy("RegisterPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(5)
                    }));

            // سیاست فراموشی رمز
            options.AddPolicy("ForgotPasswordPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(10)
                    }));

            // سیاست تغییر رمز
            options.AddPolicy("ChangePasswordPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(5)
                    }));

            // سیاست بازنشانی رمز
            options.AddPolicy("ResetPasswordPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(5)
                    }));

            // سیاست ارسال OTP
            options.AddPolicy("SendOTPPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(5)
                    }));

            // سیاست تایید OTP
            options.AddPolicy("VerifyOTPPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(5)
                    }));

            // سیاست تنظیم TOTP
            options.AddPolicy("SetupTOTPPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 2,
                        Window = TimeSpan.FromMinutes(10)
                    }));

            // سیاست تایید TOTP
            options.AddPolicy("VerifyTOTPPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(5)
                    }));

            // سیاست تجدید توکن
            options.AddPolicy("RefreshTokenPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            // سیاست ورود دو مرحله‌ای
            options.AddPolicy("LoginStep1Policy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            options.AddPolicy("LoginStep2Policy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(5)
                    }));

            options.AddPolicy("LoginStep3Policy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(5)
                    }));

            // سیاست بروزرسانی پروفایل
            options.AddPolicy("UpdateProfilePolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(5)
                    }));

            // سیاست بروزرسانی کاربر (ادمین)
            options.AddPolicy("UpdateUserPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var response = new
                {
                    success = false,
                    message = "تعداد درخواست‌های شما بیش از حد مجاز است. لطفاً بعداً تلاش کنید.",
                    retryAfter = 60
                };

                await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
            };
        });

        return services;
    }
}

// ============================================================
// پیاده‌سازی پیش‌فرض Providers
// ============================================================

public class DefaultEmailProvider : IEmailProvider
{
    public Task<bool> SendOTPAsync(string email, string code)
    {
        // پیاده‌سازی واقعی ارسال ایمیل با استفاده از SmtpClient یا SendGrid یا غیره
        Console.WriteLine($"[EMAIL] Sending OTP {code} to {email}");
        return Task.FromResult(true);
    }

    public Task<bool> SendResetPasswordLinkAsync(string email, string token)
    {
        // پیاده‌سازی واقعی ارسال ایمیل
        Console.WriteLine($"[EMAIL] Sending reset link to {email}");
        return Task.FromResult(true);
    }
}

public class DefaultSMSProvider : ISMSProvider
{
    public Task<bool> SendOTPAsync(string phoneNumber, string code)
    {
        // پیاده‌سازی واقعی ارسال پیامک با استفاده از Kavenegar, Twilio, etc.
        Console.WriteLine($"[SMS] Sending OTP {code} to {phoneNumber}");
        return Task.FromResult(true);
    }

    public Task<bool> SendResetPasswordLinkAsync(string phoneNumber, string token)
    {
        // پیاده‌سازی واقعی ارسال پیامک
        Console.WriteLine($"[SMS] Sending reset link to {phoneNumber}");
        return Task.FromResult(true);
    }
}