using AuthenticationRcl.ViewModels.Auth;
using AuthenticationRcl.ViewModels.User;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationRcl.Tests;

public class AuthServiceTests : TestBase
{
    #region Register

    [Fact]
    public async Task Register_Valid_Success()
    {
        var req = new RegisterRequest
        {
            Username = $"user{Guid.NewGuid():N}".Substring(0, 10),
            Email = $"test{Guid.NewGuid():N}@example.com",
            PhoneNumber = $"0912{Guid.NewGuid():N}".Substring(0, 11),
            Password = "Test@123"
        };

        var res = await _authService.RegisterAsync(req);
        Assert.True(res.Success, $"Expected Success but got: {res.Message}");
        Assert.NotNull(res.AccessToken);
        Assert.NotNull(res.RefreshToken);
        Assert.True(res.UserId > 0);
        Assert.Equal(req.Username, res.Username);
    }

    [Fact]
    public async Task Register_DuplicateUsername_Fail()
    {
        var username = $"user{Guid.NewGuid():N}".Substring(0, 10);
        var req1 = new RegisterRequest { Username = username, Email = $"test1{Guid.NewGuid():N}@example.com", PhoneNumber = $"0912{Guid.NewGuid():N}".Substring(0, 11), Password = "Test@123" };
        await _authService.RegisterAsync(req1);

        var req2 = new RegisterRequest { Username = username, Email = $"test2{Guid.NewGuid():N}@example.com", PhoneNumber = $"0912{Guid.NewGuid():N}".Substring(0, 11), Password = "Test@123" };
        var res = await _authService.RegisterAsync(req2);
        Assert.False(res.Success);
        Assert.Contains("نام کاربری", res.Message);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Fail()
    {
        var email = $"dup{Guid.NewGuid():N}@example.com";
        var req1 = new RegisterRequest { Username = $"user1{Guid.NewGuid():N}".Substring(0, 10), Email = email, PhoneNumber = $"0912{Guid.NewGuid():N}".Substring(0, 11), Password = "Test@123" };
        await _authService.RegisterAsync(req1);

        var req2 = new RegisterRequest { Username = $"user2{Guid.NewGuid():N}".Substring(0, 10), Email = email, PhoneNumber = $"0912{Guid.NewGuid():N}".Substring(0, 11), Password = "Test@123" };
        var res = await _authService.RegisterAsync(req2);
        Assert.False(res.Success);
        Assert.Contains("ایمیل", res.Message);
    }

    [Fact]
    public async Task Register_DuplicatePhone_Fail()
    {
        var phone = $"0912{Guid.NewGuid():N}".Substring(0, 11);
        var req1 = new RegisterRequest { Username = $"user1{Guid.NewGuid():N}".Substring(0, 10), Email = $"test1{Guid.NewGuid():N}@example.com", PhoneNumber = phone, Password = "Test@123" };
        await _authService.RegisterAsync(req1);

        var req2 = new RegisterRequest { Username = $"user2{Guid.NewGuid():N}".Substring(0, 10), Email = $"test2{Guid.NewGuid():N}@example.com", PhoneNumber = phone, Password = "Test@123" };
        var res = await _authService.RegisterAsync(req2);
        Assert.False(res.Success);
        Assert.Contains("موبایل", res.Message);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("")]
    public async Task Register_ShortPassword_Fail(string pass)
    {
        var req = new RegisterRequest
        {
            Username = $"user{Guid.NewGuid():N}".Substring(0, 10),
            Email = $"test{Guid.NewGuid():N}@example.com",
            PhoneNumber = $"0912{Guid.NewGuid():N}".Substring(0, 11),
            Password = pass
        };
        var res = await _authService.RegisterAsync(req);
        Assert.False(res.Success);
        Assert.Contains("۸ کاراکتر", res.Message);
    }

    [Fact]
    public async Task Register_ShortUsername_Fail()
    {
        var req = new RegisterRequest
        {
            Username = "ab",
            Email = $"test{Guid.NewGuid():N}@example.com",
            PhoneNumber = $"0912{Guid.NewGuid():N}".Substring(0, 11),
            Password = "Test@123"
        };
        var res = await _authService.RegisterAsync(req);
        Assert.False(res.Success);
        Assert.Contains("۳ کاراکتر", res.Message);
    }

    [Fact]
    public async Task Register_NoUsername_Fail()
    {
        var req = new RegisterRequest
        {
            Username = "",
            Email = $"test{Guid.NewGuid():N}@example.com",
            PhoneNumber = $"0912{Guid.NewGuid():N}".Substring(0, 11),
            Password = "Test@123"
        };
        var res = await _authService.RegisterAsync(req);
        Assert.False(res.Success);
        Assert.Contains("نام کاربری", res.Message);
    }

    #endregion

    #region Login - Step 1

    [Fact]
    public async Task LoginStep1_Valid_Success()
    {
        var user = await CreateUserWithTwoFactorAsync();

        var loginReq = new LoginStep1Request
        {
            Username = user.Username,
            Password = "Test@123"
        };

        var res = await _authService.LoginStep1Async(loginReq);
        Assert.True(res.Success, $"Expected Success but got: {res.Message}");
        Assert.NotNull(res.LoginToken);
        Assert.True(res.ExpiresIn > 0);
        Assert.NotEmpty(res.AvailableMethods);
    }

    [Fact]
    public async Task LoginStep1_WrongPassword_Fail()
    {
        var username = $"user{Guid.NewGuid():N}".Substring(0, 10);
        var req = new RegisterRequest
        {
            Username = username,
            Email = $"test{Guid.NewGuid():N}@example.com",
            PhoneNumber = $"0912{Guid.NewGuid():N}".Substring(0, 11),
            Password = "Test@123"
        };
        await _authService.RegisterAsync(req);

        var loginReq = new LoginStep1Request
        {
            Username = username,
            Password = "wrong"
        };

        var res = await _authService.LoginStep1Async(loginReq);
        Assert.False(res.Success);
        Assert.Contains("اشتباه", res.Message);
    }

    [Fact]
    public async Task LoginStep1_NotFound_Fail()
    {
        var loginReq = new LoginStep1Request
        {
            Username = "notexists",
            Password = "Test@123"
        };

        var res = await _authService.LoginStep1Async(loginReq);
        Assert.False(res.Success);
        Assert.Contains("اشتباه", res.Message);
    }

    [Fact]
    public async Task LoginStep1_LockAfter5Attempts()
    {
        var username = $"user{Guid.NewGuid():N}".Substring(0, 10);
        var req = new RegisterRequest
        {
            Username = username,
            Email = $"test{Guid.NewGuid():N}@example.com",
            PhoneNumber = $"0912{Guid.NewGuid():N}".Substring(0, 11),
            Password = "Test@123"
        };
        await _authService.RegisterAsync(req);

        var loginReq = new LoginStep1Request
        {
            Username = username,
            Password = "wrong"
        };

        LoginStep1Response res = null;
        for (int i = 0; i < 5; i++)
        {
            res = await _authService.LoginStep1Async(loginReq);
        }
        Assert.False(res.Success);
        Assert.Contains("قفل", res.Message);
    }

    [Fact]
    public async Task LoginStep1_InactiveUser_Fail()
    {
        var user = await CreateUserAsync();
        user.IsActive = false;
        await _db.SaveChangesAsync();

        var loginReq = new LoginStep1Request
        {
            Username = user.Username,
            Password = "Test@123"
        };

        var res = await _authService.LoginStep1Async(loginReq);
        Assert.False(res.Success);
        Assert.Contains("غیرفعال", res.Message);
    }

    #endregion

    #region Login - Step 2

    [Fact]
    public async Task LoginStep2_Email_Success()
    {
        // ✅ کاربر با ایمیل تایید شده و 2FA فعال
        var user = await CreateUserAsync(
            isEmailConfirmed: true,
            twoFactorEnabled: true,
            twoFactorMethod: "Email"
        );

        // Step 1
        var step1Req = new LoginStep1Request
        {
            Username = user.Username,
            Password = "Test@123"
        };
        var step1Res = await _authService.LoginStep1Async(step1Req);
        Assert.True(step1Res.Success, $"Step1 failed: {step1Res.Message}");

        // Step 2
        var step2Req = new LoginStep2Request
        {
            LoginToken = step1Res.LoginToken,
            Method = "Email"
        };
        var step2Res = await _authService.LoginStep2Async(step2Req);
        Assert.True(step2Res.Success, $"Step2 failed: {step2Res.Message}");
        Assert.Equal("Email", step2Res.Method);
        Assert.NotNull(step2Res.MaskedDestination);
    }

    [Fact]
    public async Task LoginStep2_SMS_Success()
    {
        // ✅ کاربر با شماره تایید شده و 2FA فعال
        var user = await CreateUserAsync(
            isPhoneConfirmed: true,
            twoFactorEnabled: true,
            twoFactorMethod: "SMS"
        );

        var step1Req = new LoginStep1Request
        {
            Username = user.Username,
            Password = "Test@123"
        };
        var step1Res = await _authService.LoginStep1Async(step1Req);
        Assert.True(step1Res.Success, $"Step1 failed: {step1Res.Message}");

        var step2Req = new LoginStep2Request
        {
            LoginToken = step1Res.LoginToken,
            Method = "SMS"
        };
        var step2Res = await _authService.LoginStep2Async(step2Req);
        Assert.True(step2Res.Success, $"Step2 failed: {step2Res.Message}");
        Assert.Equal("SMS", step2Res.Method);
        Assert.NotNull(step2Res.MaskedDestination);
    }
    [Fact]
    public async Task LoginStep2_InvalidToken_Fail()
    {
        var step2Req = new LoginStep2Request
        {
            LoginToken = "invalid-token",
            Method = "Email"
        };
        var res = await _authService.LoginStep2Async(step2Req);
        Assert.False(res.Success);
        Assert.Contains("نامعتبر", res.Message);
    }

    [Fact]
    public async Task LoginStep2_InvalidMethod_Fail()
    {
        var user = await CreateUserAsync(
            isEmailConfirmed: true,
            twoFactorEnabled: true,
            twoFactorMethod: "Email"
        );

        var step1Req = new LoginStep1Request
        {
            Username = user.Username,
            Password = "Test@123"
        };
        var step1Res = await _authService.LoginStep1Async(step1Req);
        Assert.True(step1Res.Success, $"Step1 failed: {step1Res.Message}");

        var step2Req = new LoginStep2Request
        {
            LoginToken = step1Res.LoginToken,
            Method = "InvalidMethod"
        };
        var res = await _authService.LoginStep2Async(step2Req);
        Assert.False(res.Success);
        Assert.Contains("معتبر", res.Message);
    }
    #endregion

    #region Login - Step 3

    [Fact]
    public async Task LoginStep3_WithInvalidCode_Fail()
    {
        var user = await CreateUserAsync(
            isEmailConfirmed: true,
            twoFactorEnabled: true,
            twoFactorMethod: "Email"
        );

        var step1Req = new LoginStep1Request
        {
            Username = user.Username,
            Password = "Test@123"
        };
        var step1Res = await _authService.LoginStep1Async(step1Req);
        Assert.True(step1Res.Success, $"Step1 failed: {step1Res.Message}");

        var step2Req = new LoginStep2Request
        {
            LoginToken = step1Res.LoginToken,
            Method = "Email"
        };
        var step2Res = await _authService.LoginStep2Async(step2Req);
        Assert.True(step2Res.Success, $"Step2 failed: {step2Res.Message}");

        // کد اشتباه - باید خطا بده
        var step3Req = new LoginStep3Request
        {
            LoginToken = step1Res.LoginToken,
            Code = "000000",
            IpAddress = "127.0.0.1"
        };
        var step3Res = await _authService.LoginStep3Async(step3Req);
        Assert.False(step3Res.Success, $"Expected failure but got success with message: {step3Res.Message}");
        Assert.Contains("کد", step3Res.Message);
    }

    [Fact]
    public async Task LoginStep3_WithValidCode_Success()
    {
        var user = await CreateUserAsync(
            isEmailConfirmed: true,
            twoFactorEnabled: true,
            twoFactorMethod: "Email"
        );

        var step1Req = new LoginStep1Request
        {
            Username = user.Username,
            Password = "Test@123"
        };
        var step1Res = await _authService.LoginStep1Async(step1Req);
        Assert.True(step1Res.Success, $"Step1 failed: {step1Res.Message}");

        var step2Req = new LoginStep2Request
        {
            LoginToken = step1Res.LoginToken,
            Method = "Email"
        };
        var step2Res = await _authService.LoginStep2Async(step2Req);
        Assert.True(step2Res.Success, $"Step2 failed: {step2Res.Message}");

        // کد صحیح (123456) - باید موفق باشه
        var step3Req = new LoginStep3Request
        {
            LoginToken = step1Res.LoginToken,
            Code = "123456",
            IpAddress = "127.0.0.1"
        };
        var step3Res = await _authService.LoginStep3Async(step3Req);
        Assert.True(step3Res.Success, $"Expected success but got: {step3Res.Message}");
        Assert.NotNull(step3Res.AccessToken);
        Assert.NotNull(step3Res.RefreshToken);
        Assert.Equal(user.UserId, step3Res.UserId);
    }

    [Fact]
    public async Task LoginStep3_WithInvalidToken_Fail()
    {
        var step3Req = new LoginStep3Request
        {
            LoginToken = "invalid-token",
            Code = "123456",
            IpAddress = "127.0.0.1"
        };
        var step3Res = await _authService.LoginStep3Async(step3Req);
        Assert.False(step3Res.Success);
        Assert.Contains("نامعتبر", step3Res.Message);
    }

    [Fact]
    public async Task LoginStep3_WithExpiredToken_Fail()
    {
        var user = await CreateUserAsync(
            isEmailConfirmed: true,
            twoFactorEnabled: true,
            twoFactorMethod: "Email"
        );

        var step1Req = new LoginStep1Request
        {
            Username = user.Username,
            Password = "Test@123"
        };
        var step1Res = await _authService.LoginStep1Async(step1Req);
        Assert.True(step1Res.Success, $"Step1 failed: {step1Res.Message}");

        // منقضی کردن توکن
        var dbUser = await _db.Users.FindAsync(user.UserId);
        dbUser.LoginTokenExpiry = DateTime.UtcNow.AddMinutes(-1);
        await _db.SaveChangesAsync();

        var step3Req = new LoginStep3Request
        {
            LoginToken = step1Res.LoginToken,
            Code = "123456",
            IpAddress = "127.0.0.1"
        };
        var step3Res = await _authService.LoginStep3Async(step3Req);
        Assert.False(step3Res.Success);
        Assert.Contains("منقضی", step3Res.Message);
    }

    #endregion

    #region ChangePassword

    [Fact]
    public async Task ChangePassword_Valid_Success()
    {
        var user = await CreateUserAsync();
        var req = new ChangePasswordRequest
        {
            UserId = user.UserId,
            CurrentPassword = "Test@123",
            NewPassword = "New@1234"
        };
        var res = await _authService.ChangePasswordAsync(req);
        Assert.True(res.Success, $"Expected Success but got: {res.Message}");
        Assert.True(res.IsChanged);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrent_Fail()
    {
        var user = await CreateUserAsync();
        var req = new ChangePasswordRequest
        {
            UserId = user.UserId,
            CurrentPassword = "wrong",
            NewPassword = "New@123"
        };
        var res = await _authService.ChangePasswordAsync(req);
        Assert.False(res.Success);
        Assert.Contains("فعلی", res.Message);
    }

    [Fact]
    public async Task ChangePassword_ShortNew_Fail()
    {
        var user = await CreateUserAsync();
        var req = new ChangePasswordRequest
        {
            UserId = user.UserId,
            CurrentPassword = "Test@123",
            NewPassword = "123"
        };
        var res = await _authService.ChangePasswordAsync(req);
        Assert.False(res.Success);
        Assert.Contains("۸ کاراکتر", res.Message);
    }

    #endregion

    #region Forgot & Reset

    [Fact]
    public async Task ForgotPassword_Valid_Success()
    {
        var user = await CreateUserAsync();
        var req = new ForgotPasswordRequest { Email = user.EmailAddress };
        var res = await _authService.ForgotPasswordAsync(req);
        Assert.True(res.Success);
        Assert.True(res.IsSent);
    }

    [Fact]
    public async Task ResetPassword_Valid_Success()
    {
        var user = await CreateUserAsync();

        var forgotReq = new ForgotPasswordRequest { Email = user.EmailAddress };
        var forgotRes = await _authService.ForgotPasswordAsync(forgotReq);
        Assert.True(forgotRes.Success, $"Forgot failed: {forgotRes.Message}");

        var dbUser = await _db.Users.FirstAsync(x => x.UserId == user.UserId);
        Assert.NotNull(dbUser.ResetPasswordToken);

        var req = new ResetPasswordRequest
        {
            Email = user.EmailAddress,
            Token = dbUser.ResetPasswordToken,
            NewPassword = "NewPassword123"  // ✅ حداقل ۸ کاراکتر
        };
        var res = await _authService.ResetPasswordAsync(req);
        Assert.True(res.Success, $"Expected Success but got: {res.Message}");
        Assert.True(res.IsReset);
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_Fail()
    {
        var user = await CreateUserAsync();
        var req = new ResetPasswordRequest
        {
            Email = user.EmailAddress,
            Token = "invalid",
            NewPassword = "New@123"
        };
        var res = await _authService.ResetPasswordAsync(req);
        Assert.False(res.Success);
        Assert.Contains("توکن", res.Message);
    }

    [Fact]
    public async Task ResetPassword_ExpiredToken_Fail()
    {
        var user = await CreateUserAsync();

        var forgotReq = new ForgotPasswordRequest { Email = user.EmailAddress };
        await _authService.ForgotPasswordAsync(forgotReq);

        var dbUser = await _db.Users.FirstAsync(x => x.UserId == user.UserId);
        dbUser.ResetPasswordTokenExpiry = DateTime.UtcNow.AddMinutes(-1);
        await _db.SaveChangesAsync();

        var req = new ResetPasswordRequest
        {
            Email = user.EmailAddress,
            Token = dbUser.ResetPasswordToken,
            NewPassword = "New@123"
        };
        var res = await _authService.ResetPasswordAsync(req);
        Assert.False(res.Success);
        Assert.Contains("منقضی", res.Message);
    }

    #endregion

    #region RefreshToken

    [Fact]
    public async Task RefreshToken_Valid_Success()
    {
        var req = new RegisterRequest
        {
            Username = $"user{Guid.NewGuid():N}".Substring(0, 10),
            Email = $"test{Guid.NewGuid():N}@example.com",
            PhoneNumber = $"0912{Guid.NewGuid():N}".Substring(0, 11),
            Password = "Test@123"
        };
        var registerResult = await _authService.RegisterAsync(req);
        Assert.True(registerResult.Success, "Register failed");

        var refreshReq = new RefreshTokenRequest { RefreshToken = registerResult.RefreshToken };
        var res = await _authService.RefreshTokenAsync(refreshReq);
        Assert.True(res.Success, $"RefreshToken failed: {res.Message}");
        Assert.NotNull(res.AccessToken);
        Assert.NotNull(res.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_Invalid_Fail()
    {
        var req = new RefreshTokenRequest { RefreshToken = "invalid" };
        var res = await _authService.RefreshTokenAsync(req);
        Assert.False(res.Success);
        Assert.Contains("نامعتبر", res.Message);
    }

    [Fact]
    public async Task RefreshToken_Expired_Fail()
    {
        var req = new RegisterRequest
        {
            Username = $"user{Guid.NewGuid():N}".Substring(0, 10),
            Email = $"test{Guid.NewGuid():N}@example.com",
            PhoneNumber = $"0912{Guid.NewGuid():N}".Substring(0, 11),
            Password = "Test@123"
        };
        var registerResult = await _authService.RegisterAsync(req);

        var user = await _db.Users.FindAsync(registerResult.UserId);
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(-1);
        await _db.SaveChangesAsync();

        var refreshReq = new RefreshTokenRequest { RefreshToken = registerResult.RefreshToken };
        var res = await _authService.RefreshTokenAsync(refreshReq);
        Assert.False(res.Success);
        Assert.Contains("منقضی", res.Message);
    }

    #endregion

    #region TwoFactor

    [Fact]
    public async Task Enable2FA_TOTP_Success()
    {
        var user = await CreateUserAsync();
        var res = await _authService.EnableTwoFactorAsync(user.UserId, "TOTP");
        Assert.True(res.Success);
        Assert.NotNull(res.SecretKey);
        Assert.NotNull(res.BackupCodes);
    }

    [Fact]
    public async Task Enable2FA_InvalidMethod_Fail()
    {
        var user = await CreateUserAsync();
        var res = await _authService.EnableTwoFactorAsync(user.UserId, "Invalid");
        Assert.False(res.Success);
        Assert.Contains("پشتیبانی", res.Message);
    }

    [Fact]
    public async Task Disable2FA_Success()
    {
        var user = await CreateUserAsync();
        await _authService.EnableTwoFactorAsync(user.UserId, "TOTP");

        var res = await _authService.DisableTwoFactorAsync(user.UserId);
        Assert.True(res.Success);
        Assert.Contains("غیرفعال", res.Message);
    }

    [Fact]
    public async Task Get2FAStatus_Success()
    {
        var user = await CreateUserAsync();
        var res = await _authService.GetTwoFactorStatusAsync(user.UserId);
        Assert.True(res.Success);
        Assert.False(res.IsEnabled);
    }

    [Fact]
    public async Task Get2FAStatus_Enabled_AfterSetup()
    {
        var user = await CreateUserAsync();
        await _authService.EnableTwoFactorAsync(user.UserId, "TOTP");

        var res = await _authService.GetTwoFactorStatusAsync(user.UserId);
        Assert.True(res.Success);
        Assert.True(res.IsEnabled);
    }

    #endregion

    #region UserInfo

    [Fact]
    public async Task GetUserInfo_Valid_Success()
    {
        var user = await CreateUserAsync();
        var res = await _authService.GetUserInfoAsync(user.UserId);
        Assert.True(res.Success);
        Assert.Equal(user.Username, res.Username);
        Assert.Equal(user.EmailAddress, res.Email);
        Assert.Equal(user.PhoneNumber, res.PhoneNumber);
    }

    [Fact]
    public async Task GetUserInfo_Invalid_Fail()
    {
        var res = await _authService.GetUserInfoAsync(99999);
        Assert.False(res.Success);
        Assert.Contains("یافت نشد", res.Message);
    }

    #endregion

    #region Logout

    [Fact]
    public async Task Logout_Success()
    {
        var user = await CreateUserAsync();
        var req = new LogoutRequest { UserId = user.UserId };
        var res = await _authService.LogoutAsync(req);
        Assert.True(res.Success);
        Assert.True(res.IsLoggedOut);
    }

    [Fact]
    public async Task Logout_ClearsRefreshToken()
    {
        var req = new RegisterRequest
        {
            Username = $"user{Guid.NewGuid():N}".Substring(0, 10),
            Email = $"test{Guid.NewGuid():N}@example.com",
            PhoneNumber = $"0912{Guid.NewGuid():N}".Substring(0, 11),
            Password = "Test@123"
        };
        var registerResult = await _authService.RegisterAsync(req);

        var user = await _db.Users.FindAsync(registerResult.UserId);
        Assert.NotNull(user.RefreshToken);

        var logoutReq = new LogoutRequest { UserId = user.UserId };
        await _authService.LogoutAsync(logoutReq);

        var dbUser = await _db.Users.FindAsync(user.UserId);
        Assert.Null(dbUser.RefreshToken);
        Assert.Null(dbUser.RefreshTokenExpiryTime);
    }

    #endregion

    #region UpdateProfile

    [Fact]
    public async Task UpdateProfile_Email_Success()
    {
        var user = await CreateUserAsync();
        var newEmail = $"new{Guid.NewGuid():N}@example.com";
        var req = new UpdateProfileRequest
        {
            Email = newEmail,
            PhoneNumber = null
        };
        var res = await _authService.UpdateProfileAsync(user.UserId, req);
        Assert.True(res.Success);
        Assert.Equal(newEmail, res.Email);
        Assert.False(res.IsEmailConfirmed);
    }

    [Fact]
    public async Task UpdateProfile_Phone_Success()
    {
        var user = await CreateUserAsync();
        var newPhone = $"0912{Guid.NewGuid():N}".Substring(0, 11);
        var req = new UpdateProfileRequest
        {
            Email = null,
            PhoneNumber = newPhone
        };
        var res = await _authService.UpdateProfileAsync(user.UserId, req);
        Assert.True(res.Success);
        Assert.Equal(newPhone, res.PhoneNumber);
        Assert.False(res.IsPhoneConfirmed);
    }

    #endregion
}