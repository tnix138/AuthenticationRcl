using AuthenticationRcl.Models;
using AuthenticationRcl.Services;
using AuthenticationRcl.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AuthenticationRcl.Tests;

public class AuthServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly AuthService _authService;
    private readonly ILogger<AuthService> _logger;
    private readonly Mock<ITokenService> _mockTokenService;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<AuthService>();

        // ساخت Mock برای ITokenService
        _mockTokenService = new Mock<ITokenService>();
        _mockTokenService
            .Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
            .Returns("fake-access-token");
        _mockTokenService
            .Setup(x => x.GenerateRefreshToken())
            .Returns("fake-refresh-token");

        _authService = new AuthService(_context, _logger, _mockTokenService.Object);
    }

    [Fact]
    public async Task RegisterAsync_Should_Succeed_With_Valid_Data()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            PhoneNumber = "09121234567",
            Password = "12345678"
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.True(result.IsSucceded);
        Assert.Equal("ثبت نام با موفقیت انجام شد", result.Message);
        Assert.True(result.UserId > 0);
        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
    }

    [Fact]
    public async Task RegisterAsync_Should_Fail_With_Duplicate_Email()
    {
        // Arrange
        var request1 = new RegisterRequest
        {
            Email = "test@example.com",
            PhoneNumber = "09121234567",
            Password = "12345678"
        };

        var request2 = new RegisterRequest
        {
            Email = "test@example.com",
            PhoneNumber = "09129876543",
            Password = "12345678"
        };

        // Act
        await _authService.RegisterAsync(request1);
        var result = await _authService.RegisterAsync(request2);

        // Assert
        Assert.False(result.IsSucceded);
        Assert.Equal("این ایمیل قبلاً ثبت نام کرده است", result.Message);
    }

    [Fact]
    public async Task RegisterAsync_Should_Fail_With_Duplicate_Phone()
    {
        // Arrange
        var request1 = new RegisterRequest
        {
            Email = "test1@example.com",
            PhoneNumber = "09121234567",
            Password = "12345678"
        };

        var request2 = new RegisterRequest
        {
            Email = "test2@example.com",
            PhoneNumber = "09121234567",
            Password = "12345678"
        };

        // Act
        await _authService.RegisterAsync(request1);
        var result = await _authService.RegisterAsync(request2);

        // Assert
        Assert.False(result.IsSucceded);
        Assert.Equal("این شماره موبایل قبلاً ثبت نام کرده است", result.Message);
    }

    [Fact]
    public async Task RegisterAsync_Should_Fail_With_Short_Password()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            PhoneNumber = "09121234567",
            Password = "123"
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.IsSucceded);
        Assert.Equal("رمز عبور باید حداقل ۸ کاراکتر باشد", result.Message);
    }

    [Fact]
    public async Task RegisterAsync_Should_Fail_With_No_Email_Or_Phone()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = null,
            PhoneNumber = null,
            Password = "12345678"
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.IsSucceded);
        Assert.Equal("حداقل یکی از ایمیل یا شماره موبایل باید وارد شود", result.Message);
    }

    [Fact]
    public async Task LoginAsync_Should_Succeed_With_Valid_Credentials()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "test@example.com",
            PhoneNumber = "09121234567",
            Password = "12345678"
        };

        await _authService.RegisterAsync(registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            PhoneNumber = null,
            Password = "12345678"
        };

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.True(result.IsSucceded);
        Assert.Equal("ورود با موفقیت انجام شد", result.Message);
        Assert.True(result.UserId > 0);
        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
    }

    [Fact]
    public async Task LoginAsync_Should_Fail_With_Wrong_Password()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "test@example.com",
            PhoneNumber = "09121234567",
            Password = "12345678"
        };

        await _authService.RegisterAsync(registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            PhoneNumber = null,
            Password = "wrongpassword"
        };

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.False(result.IsSucceded);
        Assert.Contains("رمز عبور اشتباه است", result.Message);
    }

    [Fact]
    public async Task LoginAsync_Should_Fail_With_NonExistent_User()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            PhoneNumber = null,
            Password = "12345678"
        };

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.False(result.IsSucceded);
        Assert.Equal("کاربری با این مشخصات یافت نشد", result.Message);
    }

    [Fact]
    public async Task LoginAsync_Should_Accept_PhoneNumber()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "test@example.com",
            PhoneNumber = "09121234567",
            Password = "12345678"
        };

        await _authService.RegisterAsync(registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = null,
            PhoneNumber = "09121234567",
            Password = "12345678"
        };

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.True(result.IsSucceded);
        Assert.Equal("ورود با موفقیت انجام شد", result.Message);
    }

    [Fact]
    public async Task LoginAsync_Should_Fail_After_5_Attempts()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "test@example.com",
            PhoneNumber = "09121234567",
            Password = "12345678"
        };

        await _authService.RegisterAsync(registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            PhoneNumber = null,
            Password = "wrongpassword"
        };

        // Act - 5 بار تلاش ناموفق
        LoginResponse result = null;
        for (int i = 0; i < 5; i++)
        {
            result = await _authService.LoginAsync(loginRequest);
        }

        // Assert
        Assert.False(result.IsSucceded);
        Assert.Contains("قفل", result.Message);
    }

    [Fact]
    public async Task ChangePasswordAsync_Should_Succeed_With_Valid_Data()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "test@example.com",
            PhoneNumber = "09121234567",
            Password = "12345678"
        };

        var registerResult = await _authService.RegisterAsync(registerRequest);

        var request = new ChangePasswordRequest
        {
            UserId = registerResult.UserId,
            CurrentPassword = "12345678",
            NewPassword = "87654321"
        };

        // Act
        var result = await _authService.ChangePasswordAsync(request);

        // Assert
        Assert.True(result.IsSucceded);
        Assert.Equal("رمز عبور با موفقیت تغییر کرد", result.Message);
        Assert.True(result.IsChanged);
    }

    [Fact]
    public async Task ChangePasswordAsync_Should_Fail_With_Wrong_CurrentPassword()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "test@example.com",
            PhoneNumber = "09121234567",
            Password = "12345678"
        };

        var registerResult = await _authService.RegisterAsync(registerRequest);

        var request = new ChangePasswordRequest
        {
            UserId = registerResult.UserId,
            CurrentPassword = "wrongpassword",
            NewPassword = "87654321"
        };

        // Act
        var result = await _authService.ChangePasswordAsync(request);

        // Assert
        Assert.False(result.IsSucceded);
        Assert.Equal("رمز عبور فعلی اشتباه است", result.Message);
    }

    [Fact]
    public async Task GetUserInfoAsync_Should_Succeed_With_Valid_UserId()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "test@example.com",
            PhoneNumber = "09121234567",
            Password = "12345678"
        };

        var registerResult = await _authService.RegisterAsync(registerRequest);

        // Act
        var result = await _authService.GetUserInfoAsync(registerResult.UserId);

        // Assert
        Assert.True(result.IsSucceded);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("09121234567", result.PhoneNumber);
        Assert.Equal("User", result.Role);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetUserInfoAsync_Should_Fail_With_Invalid_UserId()
    {
        // Act
        var result = await _authService.GetUserInfoAsync(999);

        // Assert
        Assert.False(result.IsSucceded);
        Assert.Equal("کاربر یافت نشد", result.Message);
    }

    [Fact]
    public async Task ForgotPasswordAsync_Should_Succeed_With_Valid_Email()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "test@example.com",
            PhoneNumber = "09121234567",
            Password = "12345678"
        };

        await _authService.RegisterAsync(registerRequest);

        var request = new ForgotPasswordRequest
        {
            Email = "test@example.com",
            PhoneNumber = null
        };

        // Act
        var result = await _authService.ForgotPasswordAsync(request);

        // Assert
        Assert.True(result.IsSucceded);
        Assert.Contains("لینک بازنشانی", result.Message);
        Assert.True(result.IsSent);
    }

    [Fact]
    public async Task ForgotPasswordAsync_Should_Fail_With_Invalid_Email()
    {
        // Arrange
        var request = new ForgotPasswordRequest
        {
            Email = "invalid@example.com",
            PhoneNumber = null
        };

        // Act
        var result = await _authService.ForgotPasswordAsync(request);

        // Assert
        Assert.False(result.IsSucceded);
        Assert.Equal("کاربری با این مشخصات یافت نشد", result.Message);
    }

    [Fact]
    public async Task ResetPasswordAsync_Should_Succeed_With_Valid_Token()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "test@example.com",
            PhoneNumber = "09121234567",
            Password = "12345678"
        };

        await _authService.RegisterAsync(registerRequest);

        var forgotRequest = new ForgotPasswordRequest
        {
            Email = "test@example.com",
            PhoneNumber = null
        };

        var forgotResult = await _authService.ForgotPasswordAsync(forgotRequest);
        var token = forgotResult.DevMessage?.Replace("Reset token: ", "");

        var request = new ResetPasswordRequest
        {
            Email = "test@example.com",
            PhoneNumber = null,
            NewPassword = "87654321",
            Token = token ?? ""
        };

        // Act
        var result = await _authService.ResetPasswordAsync(request);

        // Assert
        Assert.True(result.IsSucceded);
        Assert.Equal("رمز عبور با موفقیت بازنشانی شد", result.Message);
        Assert.True(result.IsReset);
    }

    [Fact]
    public async Task ResetPasswordAsync_Should_Fail_With_Invalid_Token()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "test@example.com",
            PhoneNumber = "09121234567",
            Password = "12345678"
        };

        await _authService.RegisterAsync(registerRequest);

        var request = new ResetPasswordRequest
        {
            Email = "test@example.com",
            PhoneNumber = null,
            NewPassword = "87654321",
            Token = "invalid-token"
        };

        // Act
        var result = await _authService.ResetPasswordAsync(request);

        // Assert
        Assert.False(result.IsSucceded);
        Assert.Equal("توکن نامعتبر است", result.Message);
    }

    [Fact]
    public async Task LogoutAsync_Should_Succeed()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "test@example.com",
            PhoneNumber = "09121234567",
            Password = "12345678"
        };

        var registerResult = await _authService.RegisterAsync(registerRequest);

        var request = new LogoutRequest
        {
            UserId = registerResult.UserId
        };

        // Act
        var result = await _authService.LogoutAsync(request);

        // Assert
        Assert.True(result.IsSucceded);
        Assert.Equal("خروج با موفقیت انجام شد", result.Message);
        Assert.True(result.IsLoggedOut);
    }

    [Fact]
    public async Task RefreshTokenAsync_Should_Succeed_With_Valid_RefreshToken()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "test@example.com",
            PhoneNumber = "09121234567",
            Password = "12345678"
        };

        var registerResult = await _authService.RegisterAsync(registerRequest);

        var request = new RefreshTokenRequest
        {
            RefreshToken = registerResult.RefreshToken ?? ""
        };

        // Act
        var result = await _authService.RefreshTokenAsync(request);

        // Assert
        Assert.True(result.IsSucceded);
        Assert.Equal("توکن با موفقیت تجدید شد", result.Message);
        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
    }

    [Fact]
    public async Task RefreshTokenAsync_Should_Fail_With_Invalid_RefreshToken()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "invalid-refresh-token"
        };

        // Act
        var result = await _authService.RefreshTokenAsync(request);

        // Assert
        Assert.False(result.IsSucceded);
        Assert.Equal("توکن نامعتبر است", result.Message);
    }

    [Fact]
    public void HashPassword_Should_Generate_Consistent_Hash()
    {
        // Arrange
        var password = "12345678";

        // Act
        var hash1 = HashPassword(password);
        var hash2 = HashPassword(password);

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.NotEqual(password, hash1);
    }

    #region متد کمکی برای تست

    private string HashPassword(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}