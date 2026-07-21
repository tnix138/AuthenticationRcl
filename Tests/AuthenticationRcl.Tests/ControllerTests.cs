using System.Security.Claims;
using AuthenticationRcl.Controllers.Api;
using AuthenticationRcl.Services;
using AuthenticationRcl.ViewModels.Auth;
using AuthenticationRcl.ViewModels.Base;
using AuthenticationRcl.ViewModels.OTP;
using AuthenticationRcl.ViewModels.TwoFactor;
using AuthenticationRcl.ViewModels.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace AuthenticationRcl.Tests;

public class ControllerTests
{
    private readonly Mock<IAuthService> _authMock;
    private readonly Mock<IOTPService> _otpMock;
    private readonly AuthController _authCtrl;
    private readonly UserController _userCtrl;
    private readonly OTPController _otpCtrl;
    private readonly TwoFactorController _tfCtrl;

    public ControllerTests()
    {
        _authMock = new Mock<IAuthService>();
        _otpMock = new Mock<IOTPService>();

        _authCtrl = new AuthController(_authMock.Object);
        _userCtrl = new UserController(_authMock.Object);
        _otpCtrl = new OTPController(_otpMock.Object);
        _tfCtrl = new TwoFactorController(_authMock.Object, _otpMock.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "1") }));

        var setupController = (ControllerBase ctrl) =>
        {
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        };

        setupController(_authCtrl);
        setupController(_userCtrl);
        setupController(_otpCtrl);
        setupController(_tfCtrl);
    }

    #region AuthController

    [Fact]
    public async Task Auth_Register_Valid_ReturnsCreated()
    {
        // Arrange
        var req = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Test@123"
        };
        var res = new RegisterResponse
        {
            Success = true,
            UserId = 1,
            Username = "testuser",
            AccessToken = "token",
            RefreshToken = "refresh"
        };
        _authMock.Setup(x => x.RegisterAsync(req)).ReturnsAsync(res);

        // Act
        var result = await _authCtrl.Register(req);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, created.StatusCode);
    }

    [Fact]
    public async Task Auth_Register_Invalid_ReturnsBadRequest()
    {
        // Arrange
        var req = new RegisterRequest();
        var res = new RegisterResponse { Success = false, Message = "Error" };
        _authMock.Setup(x => x.RegisterAsync(req)).ReturnsAsync(res);

        // Act
        var result = await _authCtrl.Register(req);

        // Assert
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, bad.StatusCode);
    }

    [Fact]
    public async Task Auth_LoginStep1_Valid_ReturnsOk()
    {
        // Arrange
        var req = new LoginStep1Request { Username = "testuser", Password = "Test@123" };
        var res = new LoginStep1Response
        {
            Success = true,
            LoginToken = "token",
            RequiresTwoFactor = true,
            AvailableMethods = new List<TwoFactorMethodInfo>
            {
                new TwoFactorMethodInfo { Type = "Email", Label = "ایمیل", IsAvailable = true }
            }
        };
        _authMock.Setup(x => x.LoginStep1Async(req)).ReturnsAsync(res);

        // Act
        var result = await _authCtrl.LoginStep1(req);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task Auth_LoginStep1_Invalid_ReturnsUnauthorized()
    {
        // Arrange
        var req = new LoginStep1Request { Username = "wrong", Password = "wrong" };
        var res = new LoginStep1Response { Success = false, Message = "Error" };
        _authMock.Setup(x => x.LoginStep1Async(req)).ReturnsAsync(res);

        // Act
        var result = await _authCtrl.LoginStep1(req);

        // Assert
        var unauth = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauth.StatusCode);
    }

    [Fact]
    public async Task Auth_LoginStep2_Valid_ReturnsOk()
    {
        // Arrange
        var req = new LoginStep2Request { LoginToken = "token", Method = "Email" };
        var res = new LoginStep2Response
        {
            Success = true,
            Method = "Email",
            MaskedDestination = "te***@example.com"
        };
        _authMock.Setup(x => x.LoginStep2Async(req)).ReturnsAsync(res);

        // Act
        var result = await _authCtrl.LoginStep2(req);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task Auth_LoginStep2_Invalid_ReturnsBadRequest()
    {
        // Arrange
        var req = new LoginStep2Request { LoginToken = "invalid", Method = "Email" };
        var res = new LoginStep2Response { Success = false, Message = "Error" };
        _authMock.Setup(x => x.LoginStep2Async(req)).ReturnsAsync(res);

        // Act
        var result = await _authCtrl.LoginStep2(req);

        // Assert
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, bad.StatusCode);
    }

    [Fact]
    public async Task Auth_LoginStep3_Valid_ReturnsOk()
    {
        // Arrange
        var req = new LoginStep3Request { LoginToken = "token", Code = "123456" };
        var res = new LoginStep3Response
        {
            Success = true,
            UserId = 1,
            Username = "testuser",
            AccessToken = "token",
            RefreshToken = "refresh"
        };
        _authMock.Setup(x => x.LoginStep3Async(req)).ReturnsAsync(res);

        // Act
        var result = await _authCtrl.LoginStep3(req);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task Auth_LoginStep3_Invalid_ReturnsUnauthorized()
    {
        // Arrange
        var req = new LoginStep3Request { LoginToken = "invalid", Code = "123456" };
        var res = new LoginStep3Response { Success = false, Message = "Error" };
        _authMock.Setup(x => x.LoginStep3Async(req)).ReturnsAsync(res);

        // Act
        var result = await _authCtrl.LoginStep3(req);

        // Assert
        var unauth = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauth.StatusCode);
    }

    [Fact]
    public async Task Auth_Logout_Valid_ReturnsNoContent()
    {
        // Arrange
        _authMock.Setup(x => x.LogoutAsync(It.IsAny<LogoutRequest>()))
            .ReturnsAsync(new LogoutResponse { Success = true });

        // Act
        var result = await _authCtrl.Logout();

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Auth_ChangePassword_Valid_ReturnsOk()
    {
        // Arrange
        var req = new ChangePasswordRequest { CurrentPassword = "old", NewPassword = "new" };
        _authMock.Setup(x => x.ChangePasswordAsync(req))
            .ReturnsAsync(new ChangePasswordResponse { Success = true });

        // Act
        var result = await _authCtrl.ChangePassword(req);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task Auth_ForgotPassword_Valid_ReturnsOk()
    {
        // Arrange
        var req = new ForgotPasswordRequest { Email = "test@example.com" };
        _authMock.Setup(x => x.ForgotPasswordAsync(req))
            .ReturnsAsync(new ForgotPasswordResponse { Success = true });

        // Act
        var result = await _authCtrl.ForgotPassword(req);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Auth_RefreshToken_Valid_ReturnsOk()
    {
        // Arrange
        var req = new RefreshTokenRequest { RefreshToken = "token" };
        _authMock.Setup(x => x.RefreshTokenAsync(req))
            .ReturnsAsync(new RefreshTokenResponse { Success = true, AccessToken = "new" });

        // Act
        var result = await _authCtrl.RefreshToken(req);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    #endregion

    #region UserController

    [Fact]
    public async Task User_GetMe_Valid_ReturnsOk()
    {
        // Arrange
        _authMock.Setup(x => x.GetUserInfoAsync(1))
            .ReturnsAsync(new UserInfoResponse
            {
                Success = true,
                Username = "testuser",
                Email = "test@example.com",
                PhoneNumber = "09121234567"
            });

        // Act
        var result = await _userCtrl.GetCurrentUserInfo();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task User_GetUser_Valid_ReturnsOk()
    {
        // Arrange
        _authMock.Setup(x => x.GetUserInfoAsync(1))
            .ReturnsAsync(new UserInfoResponse { Success = true, Username = "testuser" });

        // Act
        var result = await _userCtrl.GetUserInfo(1);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task User_GetUser_NotFound_ReturnsNotFound()
    {
        // Arrange
        _authMock.Setup(x => x.GetUserInfoAsync(999))
            .ReturnsAsync(new UserInfoResponse { Success = false, Message = "Not found" });

        // Act
        var result = await _userCtrl.GetUserInfo(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task User_GetUserByUsername_Valid_ReturnsOk()
    {
        // Arrange
        _authMock.Setup(x => x.GetUserInfoByUsernameAsync("testuser"))
            .ReturnsAsync(new UserInfoResponse { Success = true, Username = "testuser" });

        // Act
        var result = await _userCtrl.GetUserInfoByUsername("testuser");

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task User_GetUserByUsername_NotFound_ReturnsNotFound()
    {
        // Arrange
        _authMock.Setup(x => x.GetUserInfoByUsernameAsync("notfound"))
            .ReturnsAsync(new UserInfoResponse { Success = false, Message = "Not found" });

        // Act
        var result = await _userCtrl.GetUserInfoByUsername("notfound");

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task User_UpdateProfile_Valid_ReturnsOk()
    {
        // Arrange
        var req = new UpdateProfileRequest { Email = "new@example.com" };
        _authMock.Setup(x => x.UpdateProfileAsync(1, req))
            .ReturnsAsync(new UpdateProfileResponse
            {
                Success = true,
                Email = "new@example.com",
                IsEmailConfirmed = false
            });

        // Act
        var result = await _userCtrl.UpdateProfile(req);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task User_UpdateProfile_Invalid_ReturnsBadRequest()
    {
        // Arrange
        var req = new UpdateProfileRequest { Email = "new@example.com" };
        _authMock.Setup(x => x.UpdateProfileAsync(1, req))
            .ReturnsAsync(new UpdateProfileResponse { Success = false, Message = "Error" });

        // Act
        var result = await _userCtrl.UpdateProfile(req);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task User_UpdateUser_Valid_ReturnsOk()
    {
        // Arrange
        var req = new UpdateUserRequest { Role = "Admin" };
        _authMock.Setup(x => x.UpdateUserAsync(1, req))
            .ReturnsAsync(new UpdateUserResponse { Success = true, UserId = 1, IsUpdated = true });

        // Act
        var result = await _userCtrl.UpdateUser(1, req);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task User_DeleteUser_Valid_ReturnsNoContent()
    {
        // Arrange
        _authMock.Setup(x => x.DeleteUserAsync(1))
            .ReturnsAsync(new ResponseBase { Success = true });

        // Act
        var result = await _userCtrl.DeleteUser(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task User_DeleteUser_NotFound_ReturnsNotFound()
    {
        // Arrange
        _authMock.Setup(x => x.DeleteUserAsync(999))
            .ReturnsAsync(new ResponseBase { Success = false, Message = "Not found" });

        // Act
        var result = await _userCtrl.DeleteUser(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region OTPController

    [Fact]
    public async Task OTP_Send_Valid_ReturnsOk()
    {
        // Arrange
        var req = new SendOTPRequest { Channel = "Email" };
        _otpMock.Setup(x => x.SendOTPAsync(req))
            .ReturnsAsync(new SendOTPResponse { Success = true, OtpId = 1 });

        // Act
        var result = await _otpCtrl.SendOTP(req);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task OTP_Send_Invalid_ReturnsBadRequest()
    {
        // Arrange
        var req = new SendOTPRequest();
        _otpMock.Setup(x => x.SendOTPAsync(req))
            .ReturnsAsync(new SendOTPResponse { Success = false, Message = "Error" });

        // Act
        var result = await _otpCtrl.SendOTP(req);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task OTP_Verify_Valid_ReturnsOk()
    {
        // Arrange
        var req = new VerifyOTPRequest { Code = "123456" };
        _otpMock.Setup(x => x.VerifyOTPAsync(req))
            .ReturnsAsync(new VerifyOTPResponse { Success = true, IsVerified = true });

        // Act
        var result = await _otpCtrl.VerifyOTP(req);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task OTP_Verify_Invalid_ReturnsBadRequest()
    {
        // Arrange
        var req = new VerifyOTPRequest { Code = "invalid" };
        _otpMock.Setup(x => x.VerifyOTPAsync(req))
            .ReturnsAsync(new VerifyOTPResponse { Success = false, Message = "Error" });

        // Act
        var result = await _otpCtrl.VerifyOTP(req);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region TwoFactorController

    [Fact]
    public async Task TwoFactor_GetStatus_ReturnsOk()
    {
        // Arrange
        _authMock.Setup(x => x.GetTwoFactorStatusAsync(1))
            .ReturnsAsync(new TwoFactorStatusResponse { Success = true, IsEnabled = false });

        // Act
        var result = await _tfCtrl.GetStatus();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task TwoFactor_SetupTOTP_Valid_ReturnsOk()
    {
        // Arrange
        _authMock.Setup(x => x.EnableTwoFactorAsync(1, "TOTP"))
            .ReturnsAsync(new TwoFactorSetupResponse
            {
                Success = true,
                SecretKey = "KEY",
                BackupCodes = new List<string> { "12345678" }
            });

        // Act
        var result = await _tfCtrl.SetupTOTP();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    [Fact]
    public async Task TwoFactor_SetupTOTP_Invalid_ReturnsBadRequest()
    {
        // Arrange
        _authMock.Setup(x => x.EnableTwoFactorAsync(1, "TOTP"))
            .ReturnsAsync(new TwoFactorSetupResponse { Success = false, Message = "Error" });

        // Act
        var result = await _tfCtrl.SetupTOTP();

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task TwoFactor_Disable_Valid_ReturnsOk()
    {
        // Arrange
        _authMock.Setup(x => x.DisableTwoFactorAsync(1))
            .ReturnsAsync(new ResponseBase { Success = true });

        // Act
        var result = await _tfCtrl.DisableTwoFactor();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
    }

    #endregion
}