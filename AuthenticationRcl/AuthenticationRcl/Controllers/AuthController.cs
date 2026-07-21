using System.Security.Claims;
using AuthenticationRcl.Services;
using AuthenticationRcl.ViewModels.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthenticationRcl.Controllers.Api;

[ApiController]
[Route("api/v1/[controller]")]
[EnableRateLimiting("GlobalPolicy")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // ثبت نام
    [HttpPost("register")]
    [EnableRateLimiting("RegisterPolicy")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return CreatedAtAction(nameof(UserController.GetUserInfo), "User", new { userId = result.UserId }, new
        {
            success = true,
            message = result.Message,
            data = new
            {
                userId = result.UserId,
                username = result.Username,
                accessToken = result.AccessToken,
                refreshToken = result.RefreshToken,
                expiresIn = result.ExpiresIn
            }
        });
    }

    // ورود مرحله اول - نام کاربری و رمز عبور
    [HttpPost("login/step1")]
    [EnableRateLimiting("LoginStep1Policy")]
    public async Task<IActionResult> LoginStep1([FromBody] LoginStep1Request request)
    {
        request.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        request.UserAgent = Request.Headers["User-Agent"].ToString();

        var result = await _authService.LoginStep1Async(request);

        if (!result.Success)
            return Unauthorized(new { success = false, message = result.Message });

        return Ok(new
        {
            success = true,
            message = result.Message,
            data = new
            {
                loginToken = result.LoginToken,
                expiresIn = result.ExpiresIn,
                requiresTwoFactor = result.RequiresTwoFactor,
                availableMethods = result.AvailableMethods,
                userInfo = result.UserInfo
            }
        });
    }

    // ورود مرحله دوم - انتخاب روش دریافت کد
    [HttpPost("login/step2")]
    [EnableRateLimiting("LoginStep2Policy")]
    public async Task<IActionResult> LoginStep2([FromBody] LoginStep2Request request)
    {
        var result = await _authService.LoginStep2Async(request);

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new
        {
            success = true,
            message = result.Message,
            data = new
            {
                method = result.Method,
                expiresIn = result.ExpiresIn,
                maskedDestination = result.MaskedDestination
            }
        });
    }

    // ورود مرحله سوم - تایید کد
    [HttpPost("login/step3")]
    [EnableRateLimiting("LoginStep3Policy")]
    public async Task<IActionResult> LoginStep3([FromBody] LoginStep3Request request)
    {
        request.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _authService.LoginStep3Async(request);

        if (!result.Success)
            return Unauthorized(new { success = false, message = result.Message });

        return Ok(new
        {
            success = true,
            message = result.Message,
            data = new
            {
                userId = result.UserId,
                username = result.Username,
                accessToken = result.AccessToken,
                refreshToken = result.RefreshToken,
                expiresIn = result.ExpiresIn
            }
        });
    }

    // خروج
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = GetCurrentUserId();
        var request = new LogoutRequest { UserId = userId };
        var result = await _authService.LogoutAsync(request);

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return NoContent();
    }

    // تغییر رمز عبور
    [HttpPut("change-password")]
    [Authorize]
    [EnableRateLimiting("ChangePasswordPolicy")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        request.UserId = GetCurrentUserId();
        var result = await _authService.ChangePasswordAsync(request);

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, message = result.Message, data = new { isChanged = result.IsChanged } });
    }

    // فراموشی رمز عبور
    [HttpPost("forgot-password")]
    [EnableRateLimiting("ForgotPasswordPolicy")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _authService.ForgotPasswordAsync(request);

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, message = result.Message, data = new { isSent = result.IsSent } });
    }

    // بازنشانی رمز عبور
    [HttpPost("reset-password")]
    [EnableRateLimiting("ResetPasswordPolicy")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request);

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, message = result.Message, data = new { isReset = result.IsReset } });
    }

    // تجدید توکن
    [HttpPost("refresh-token")]
    [EnableRateLimiting("RefreshTokenPolicy")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);

        if (!result.Success)
            return Unauthorized(new { success = false, message = result.Message });

        return Ok(new
        {
            success = true,
            message = result.Message,
            data = new
            {
                accessToken = result.AccessToken,
                refreshToken = result.RefreshToken,
                expiresIn = result.ExpiresIn
            }
        });
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? int.Parse(claim.Value) : 0;
    }
}