using AuthenticationRcl.Models;
using AuthenticationRcl.Options;
using AuthenticationRcl.Services;
using AuthenticationRcl.ViewModels.Base;
using AuthenticationRcl.ViewModels.OTP;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using BCrypt.Net;

namespace AuthenticationRcl.Tests;

public class TestBase : IDisposable
{
    protected readonly AppDbContext _db;
    protected readonly AuthService _authService;
    protected readonly Mock<ITokenService> _tokenMock;
    protected readonly Mock<IOTPService> _otpMock;
    private readonly string _dbName;

    public TestBase()
    {
        _dbName = Guid.NewGuid().ToString();

        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_dbName)
            .Options);

        _tokenMock = new Mock<ITokenService>();
        _tokenMock.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("mock-access-token");
        _tokenMock.Setup(x => x.GenerateRefreshToken()).Returns("mock-refresh-token");
        _tokenMock.Setup(x => x.GetAccessTokenExpirySeconds()).Returns(900);
        _tokenMock.Setup(x => x.ValidateToken(It.IsAny<string>())).Returns(true);
        _tokenMock.Setup(x => x.GetUserIdFromToken(It.IsAny<string>())).Returns(1);

        _otpMock = new Mock<IOTPService>();

        // SendOTPAsync
        _otpMock.Setup(x => x.SendOTPAsync(It.IsAny<SendOTPRequest>()))
            .ReturnsAsync(new SendOTPResponse { Success = true, OtpId = 1 });

        // ✅ VerifyOTPAsync - فقط کد 123456 رو قبول کن
        _otpMock.Setup(x => x.VerifyOTPAsync(It.IsAny<VerifyOTPRequest>()))
            .ReturnsAsync((VerifyOTPRequest request) =>
            {
                if (request.Code == "123456")
                    return new VerifyOTPResponse { Success = true, IsVerified = true };
                else
                    return new VerifyOTPResponse { Success = false, Message = "کد نامعتبر است", IsVerified = false };
            });

        // SetupTOTPAsync
        _otpMock.Setup(x => x.SetupTOTPAsync(It.IsAny<int>()))
            .ReturnsAsync(new TOTPSetupResult
            {
                IsSuccess = true,
                SecretKey = "JBSWY3DPEHPK3PXP",
                QRCodeUrl = "otpauth://totp/App:test@example.com",
                BackupCodes = new List<string> { "12345678", "87654321", "11223344" }
            });

        // VerifyTOTPAsync - فقط کد 123456 رو قبول کن
        _otpMock.Setup(x => x.VerifyTOTPAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync((int userId, string code) => code == "123456");

        _otpMock.Setup(x => x.DisableTOTPAsync(It.IsAny<int>())).ReturnsAsync(true);
        _otpMock.Setup(x => x.GenerateBackupCodesAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<string> { "12345678", "87654321" });

        var authOptions = new AuthOptions
        {
            MinimumPasswordLength = 8,
            MaxFailedAttempts = 5,
            LockoutMinutes = 15,
            RefreshTokenExpiryDays = 30,
            ResetTokenExpiryHours = 1,
            LoginTokenExpiryMinutes = 5,
            OTPExpiryMinutes = 5,
            BCryptWorkFactor = 12
        };

        var logger = new LoggerFactory().CreateLogger<AuthService>();

        _authService = new AuthService(
            _db,
            logger,
            _tokenMock.Object,
            _otpMock.Object,
            new OptionsWrapper<AuthOptions>(authOptions));
    }

    // ============================================================
    // متدهای کمکی
    // ============================================================

    protected async Task<User> CreateUserAsync(
        string? email = null,
        string? phone = null,
        bool isEmailConfirmed = false,
        bool isPhoneConfirmed = false,
        bool twoFactorEnabled = false,
        string? twoFactorMethod = null)
    {
        var user = new User
        {
            Username = $"user{Guid.NewGuid():N}".Substring(0, 10),
            EmailAddress = email ?? $"test{Guid.NewGuid():N}@example.com",
            PhoneNumber = phone ?? $"0912{Guid.NewGuid():N}".Substring(0, 11),
            Password = BCrypt.Net.BCrypt.HashPassword("Test@123"),
            Role = "User",
            IsActive = true,
            IsEmailConfirmed = isEmailConfirmed,
            IsPhoneConfirmed = isPhoneConfirmed,
            TwoFactorEnabled = twoFactorEnabled,
            TwoFactorMethod = twoFactorMethod,
            CreatedAt = DateTime.UtcNow
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();
        return user;
    }

    protected async Task<User> CreateUserWithPasswordAsync(string username, string password)
    {
        var user = new User
        {
            Username = username,
            EmailAddress = $"test{Guid.NewGuid():N}@example.com",
            PhoneNumber = $"0912{Guid.NewGuid():N}".Substring(0, 11),
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "User",
            IsActive = true,
            IsEmailConfirmed = true,
            IsPhoneConfirmed = true,
            TwoFactorEnabled = true,
            TwoFactorMethod = "Email",
            CreatedAt = DateTime.UtcNow
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();
        return user;
    }

    protected async Task<User> CreateUserWithTwoFactorAsync(string? username = null)
    {
        var user = new User
        {
            Username = username ?? $"user{Guid.NewGuid():N}".Substring(0, 10),
            EmailAddress = $"test{Guid.NewGuid():N}@example.com",
            PhoneNumber = $"0912{Guid.NewGuid():N}".Substring(0, 11),
            Password = BCrypt.Net.BCrypt.HashPassword("Test@123"),
            Role = "User",
            IsActive = true,
            IsEmailConfirmed = true,
            IsPhoneConfirmed = true,
            TwoFactorEnabled = true,
            TwoFactorMethod = "TOTP",
            TwoFactorSecret = "JBSWY3DPEHPK3PXP",
            CreatedAt = DateTime.UtcNow
        };
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();
        return user;
    }

    protected T Error<T>(string msg) where T : ResponseBase, new()
    {
        return new T { Success = false, Message = msg };
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}