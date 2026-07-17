using AuthenticationRcl.Controllers.Api;
using AuthenticationRcl.Services;
using AuthenticationRcl.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Text.Json;
using Xunit;

namespace AuthenticationRcl.Tests;

/// <summary>
/// تست‌های واحد برای کنترلر UsersController
/// </summary>
public class UsersControllerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _controller = new UsersController(_mockAuthService.Object);

        // تنظیم HttpContext برای تست
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        httpContext.Request.Headers["User-Agent"] = "Test Agent";

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region Register - ثبت نام

    [Fact]
    public async Task Register_Should_Return_Created_When_Successful()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            PhoneNumber = "09123456789",
            Password = "12345678"
        };

        var expectedResponse = new RegisterResponse
        {
            IsSucceded = true,
            Message = "ثبت نام با موفقیت انجام شد",
            UserId = 1,
            AccessToken = "fake-token",
            RefreshToken = "fake-refresh-token",
            ExpiresIn = 900
        };

        _mockAuthService
            .Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(UsersController.GetUserInfo), createdResult.ActionName);

        var json = JsonSerializer.Serialize(createdResult.Value);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("ثبت نام با موفقیت انجام شد", root.GetProperty("message").GetString());
        Assert.Equal(1, root.GetProperty("data").GetProperty("userId").GetInt32());
        Assert.NotNull(root.GetProperty("data").GetProperty("accessToken").GetString());
        Assert.NotNull(root.GetProperty("data").GetProperty("refreshToken").GetString());
    }

    [Fact]
    public async Task Register_Should_Return_BadRequest_When_Failed()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            PhoneNumber = "09123456789",
            Password = "123"
        };

        var expectedResponse = new RegisterResponse
        {
            IsSucceded = false,
            Message = "رمز عبور باید حداقل ۸ کاراکتر باشد",
            DevMessage = "Password too short"
        };

        _mockAuthService
            .Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var json = JsonSerializer.Serialize(badRequestResult.Value);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal("رمز عبور باید حداقل ۸ کاراکتر باشد", root.GetProperty("message").GetString());
    }

    #endregion

    #region GetUserInfo - دریافت اطلاعات کاربر

    [Fact]
    public async Task GetUserInfo_Should_Return_Ok_When_User_Exists()
    {
        // Arrange
        var userId = 1;
        var expectedResponse = new UserInfoResponse
        {
            IsSucceded = true,
            Message = "اطلاعات کاربر دریافت شد",
            UserId = userId,
            Email = "test@example.com",
            PhoneNumber = "09123456789",
            Role = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        _mockAuthService
            .Setup(x => x.GetUserInfoAsync(userId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetUserInfo(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("test@example.com", root.GetProperty("data").GetProperty("email").GetString());
        Assert.Equal("09123456789", root.GetProperty("data").GetProperty("phoneNumber").GetString());
    }

    [Fact]
    public async Task GetUserInfo_Should_Return_NotFound_When_User_DoesNot_Exist()
    {
        // Arrange
        var userId = 999;
        var expectedResponse = new UserInfoResponse
        {
            IsSucceded = false,
            Message = "کاربر یافت نشد",
            DevMessage = "User not found"
        };

        _mockAuthService
            .Setup(x => x.GetUserInfoAsync(userId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetUserInfo(userId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var json = JsonSerializer.Serialize(notFoundResult.Value);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal("کاربر یافت نشد", root.GetProperty("message").GetString());
    }

    #endregion

    #region Login - ورود

    [Fact]
    public async Task Login_Should_Return_Ok_When_Credentials_Are_Valid()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "12345678"
        };

        var expectedResponse = new LoginResponse
        {
            IsSucceded = true,
            Message = "ورود با موفقیت انجام شد",
            UserId = 1,
            AccessToken = "fake-token",
            RefreshToken = "fake-refresh-token",
            ExpiresIn = 900
        };

        _mockAuthService
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("ورود با موفقیت انجام شد", root.GetProperty("message").GetString());
        Assert.Equal(1, root.GetProperty("data").GetProperty("userId").GetInt32());
        Assert.NotNull(root.GetProperty("data").GetProperty("accessToken").GetString());
        Assert.NotNull(root.GetProperty("data").GetProperty("refreshToken").GetString());
    }

    [Fact]
    public async Task Login_Should_Return_Unauthorized_When_Credentials_Are_Invalid()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "wrongpassword"
        };

        var expectedResponse = new LoginResponse
        {
            IsSucceded = false,
            Message = "رمز عبور اشتباه است",
            DevMessage = "Invalid password"
        };

        _mockAuthService
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var json = JsonSerializer.Serialize(unauthorizedResult.Value);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal("رمز عبور اشتباه است", root.GetProperty("message").GetString());
    }

    #endregion

    #region ChangePassword - تغییر رمز عبور

    [Fact]
    public async Task ChangePassword_Should_Return_Ok_When_Successful()
    {
        // Arrange
        var userId = 1;
        var request = new ChangePasswordRequest
        {
            UserId = userId,
            CurrentPassword = "12345678",
            NewPassword = "87654321"
        };

        var expectedResponse = new ChangePasswordResponse
        {
            IsSucceded = true,
            Message = "رمز عبور با موفقیت تغییر کرد",
            IsChanged = true
        };

        _mockAuthService
            .Setup(x => x.ChangePasswordAsync(It.IsAny<ChangePasswordRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ChangePassword(userId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("رمز عبور با موفقیت تغییر کرد", root.GetProperty("message").GetString());
        Assert.True(root.GetProperty("data").GetProperty("isChanged").GetBoolean());
    }

    [Fact]
    public async Task ChangePassword_Should_Return_BadRequest_When_UserId_Mismatch()
    {
        // Arrange
        var userId = 1;
        var request = new ChangePasswordRequest
        {
            UserId = 2,
            CurrentPassword = "12345678",
            NewPassword = "87654321"
        };

        // Act
        var result = await _controller.ChangePassword(userId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var json = JsonSerializer.Serialize(badRequestResult.Value);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Contains("مطابقت ندارد", root.GetProperty("message").GetString());
    }

    [Fact]
    public async Task ChangePassword_Should_Return_BadRequest_When_Failed()
    {
        // Arrange
        var userId = 1;
        var request = new ChangePasswordRequest
        {
            UserId = userId,
            CurrentPassword = "wrongpassword",
            NewPassword = "87654321"
        };

        var expectedResponse = new ChangePasswordResponse
        {
            IsSucceded = false,
            Message = "رمز عبور فعلی اشتباه است",
            DevMessage = "Current password is incorrect"
        };

        _mockAuthService
            .Setup(x => x.ChangePasswordAsync(It.IsAny<ChangePasswordRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ChangePassword(userId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var json = JsonSerializer.Serialize(badRequestResult.Value);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal("رمز عبور فعلی اشتباه است", root.GetProperty("message").GetString());
    }

    #endregion

    #region ForgotPassword - فراموشی رمز

    [Fact]
    public async Task ForgotPassword_Should_Return_Ok_When_Successful()
    {
        // Arrange
        var request = new ForgotPasswordRequest
        {
            Email = "test@example.com"
        };

        var expectedResponse = new ForgotPasswordResponse
        {
            IsSucceded = true,
            Message = "لینک بازنشانی رمز عبور به ایمیل شما ارسال شد",
            IsSent = true
        };

        _mockAuthService
            .Setup(x => x.ForgotPasswordAsync(It.IsAny<ForgotPasswordRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("لینک بازنشانی رمز عبور به ایمیل شما ارسال شد", root.GetProperty("message").GetString());
        Assert.True(root.GetProperty("data").GetProperty("isSent").GetBoolean());
    }

    [Fact]
    public async Task ForgotPassword_Should_Return_BadRequest_When_Failed()
    {
        // Arrange
        var request = new ForgotPasswordRequest
        {
            Email = "invalid@example.com"
        };

        var expectedResponse = new ForgotPasswordResponse
        {
            IsSucceded = false,
            Message = "کاربری با این مشخصات یافت نشد",
            DevMessage = "User not found"
        };

        _mockAuthService
            .Setup(x => x.ForgotPasswordAsync(It.IsAny<ForgotPasswordRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var json = JsonSerializer.Serialize(badRequestResult.Value);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal("کاربری با این مشخصات یافت نشد", root.GetProperty("message").GetString());
    }

    #endregion

    #region ResetPassword - بازنشانی رمز

    [Fact]
    public async Task ResetPassword_Should_Return_Ok_When_Successful()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            Email = "test@example.com",
            Token = "valid-token",
            NewPassword = "87654321"
        };

        var expectedResponse = new ResetPasswordResponse
        {
            IsSucceded = true,
            Message = "رمز عبور با موفقیت بازنشانی شد",
            IsReset = true
        };

        _mockAuthService
            .Setup(x => x.ResetPasswordAsync(It.IsAny<ResetPasswordRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("رمز عبور با موفقیت بازنشانی شد", root.GetProperty("message").GetString());
        Assert.True(root.GetProperty("data").GetProperty("isReset").GetBoolean());
    }

    [Fact]
    public async Task ResetPassword_Should_Return_BadRequest_When_Failed()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            Email = "test@example.com",
            Token = "invalid-token",
            NewPassword = "87654321"
        };

        var expectedResponse = new ResetPasswordResponse
        {
            IsSucceded = false,
            Message = "توکن نامعتبر است",
            DevMessage = "Invalid token"
        };

        _mockAuthService
            .Setup(x => x.ResetPasswordAsync(It.IsAny<ResetPasswordRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var json = JsonSerializer.Serialize(badRequestResult.Value);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal("توکن نامعتبر است", root.GetProperty("message").GetString());
    }

    #endregion

    #region RefreshToken - تجدید توکن

    [Fact]
    public async Task RefreshToken_Should_Return_Ok_When_Successful()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "valid-refresh-token"
        };

        var expectedResponse = new RefreshTokenResponse
        {
            IsSucceded = true,
            Message = "توکن با موفقیت تجدید شد",
            AccessToken = "new-access-token",
            RefreshToken = "new-refresh-token",
            ExpiresIn = 900
        };

        _mockAuthService
            .Setup(x => x.RefreshTokenAsync(It.IsAny<RefreshTokenRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.RefreshToken(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("توکن با موفقیت تجدید شد", root.GetProperty("message").GetString());
        Assert.NotNull(root.GetProperty("data").GetProperty("accessToken").GetString());
        Assert.NotNull(root.GetProperty("data").GetProperty("refreshToken").GetString());
    }

    [Fact]
    public async Task RefreshToken_Should_Return_Unauthorized_When_Failed()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "invalid-refresh-token"
        };

        var expectedResponse = new RefreshTokenResponse
        {
            IsSucceded = false,
            Message = "توکن نامعتبر است",
            DevMessage = "Invalid refresh token"
        };

        _mockAuthService
            .Setup(x => x.RefreshTokenAsync(It.IsAny<RefreshTokenRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.RefreshToken(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var json = JsonSerializer.Serialize(unauthorizedResult.Value);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal("توکن نامعتبر است", root.GetProperty("message").GetString());
    }

    #endregion

    #region UpdateUser - بروزرسانی کاربر

    [Fact]
    public async Task UpdateUser_Should_Return_Ok()
    {
        // Arrange
        var userId = 1;
        var request = new UpdateUserRequest
        {
            Email = "updated@example.com",
            PhoneNumber = "0987654321",
            IsActive = true
        };

        // Act
        var result = await _controller.UpdateUser(userId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(1, root.GetProperty("data").GetProperty("userId").GetInt32());
        Assert.True(root.GetProperty("data").GetProperty("isUpdated").GetBoolean());
    }

    #endregion

    #region DeleteUser - حذف کاربر

    [Fact]
    public async Task DeleteUser_Should_Return_NoContent()
    {
        // Arrange
        var userId = 1;

        // Act
        var result = await _controller.DeleteUser(userId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    #endregion

    #region Logout - خروج

    [Fact]
    public async Task Logout_Should_Return_NoContent()
    {
        // Arrange
        var sessionId = 1;

        // Act
        var result = await _controller.Logout(sessionId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    #endregion
}