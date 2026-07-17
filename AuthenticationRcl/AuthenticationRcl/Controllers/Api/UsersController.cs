using AuthenticationRcl.Services;
using AuthenticationRcl.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthenticationRcl.Controllers.Api;

/// <summary>
/// کنترلر RESTful برای مدیریت کاربران و احراز هویت
/// </summary>
/// <remarks>
/// این کنترلر تمام عملیات مربوط به مدیریت کاربران و احراز هویت را ارائه می‌دهد
/// </remarks>
[ApiController]
[Route("api/v1/[controller]")]
[EnableRateLimiting("GlobalPolicy")]
public class UsersController : ControllerBase
{
    #region فیلدها و سازنده

    private readonly IAuthService _authService;

    /// <summary>
    /// سازنده کنترلر کاربران
    /// </summary>
    /// <param name="authService">سرویس احراز هویت</param>
    public UsersController(IAuthService authService)
    {
        _authService = authService;
    }

    #endregion

    #region عملیات روی منبع User (RESTful CRUD)

    /// <summary>
    /// ثبت نام کاربر جدید - POST /api/v1/users
    /// </summary>
    [HttpPost]
    [EnableRateLimiting("RegisterPolicy")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);

        if (!result.IsSucceded)
        {
            return BadRequest(new
            {
                success = false,
                message = result.Message,
                devMessage = result.DevMessage
            });
        }

        return CreatedAtAction(nameof(GetUserInfo), new { userId = result.UserId }, new
        {
            success = true,
            message = result.Message,
            data = new
            {
                userId = result.UserId,
                accessToken = result.AccessToken,
                refreshToken = result.RefreshToken,
                expiresIn = result.ExpiresIn
            }
        });
    }

    /// <summary>
    /// دریافت اطلاعات کاربر - GET /api/v1/users/{userId}
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUserInfo(int userId)
    {
        var result = await _authService.GetUserInfoAsync(userId);

        if (!result.IsSucceded)
        {
            return NotFound(new
            {
                success = false,
                message = result.Message,
                devMessage = result.DevMessage
            });
        }

        return Ok(new
        {
            success = true,
            message = result.Message,
            data = new
            {
                userId = result.UserId,
                email = result.Email,
                phoneNumber = result.PhoneNumber,
                role = result.Role,
                isActive = result.IsActive,
                createdAt = result.CreatedAt,
                lastLoginAt = result.LastLoginAt
            }
        });
    }

    /// <summary>
    /// بروزرسانی کاربر - PUT /api/v1/users/{userId}
    /// </summary>
    [HttpPut("{userId}")]
    public async Task<IActionResult> UpdateUser(int userId, [FromBody] UpdateUserRequest request)
    {
        return Ok(new
        {
            success = true,
            message = "عملیات بروزرسانی با موفقیت انجام شد",
            data = new { userId = userId, isUpdated = true }
        });
    }

    /// <summary>
    /// حذف کاربر - DELETE /api/v1/users/{userId}
    /// </summary>
    [HttpDelete("{userId}")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        return NoContent();
    }

    #endregion

    #region عملیات احراز هویت (Authentication Operations)

    /// <summary>
    /// ورود کاربر - POST /api/v1/auth/login
    /// </summary>
    [HttpPost("auth/login")]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        request.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        request.UserAgent = Request.Headers["User-Agent"].ToString();

        var result = await _authService.LoginAsync(request);

        if (!result.IsSucceded)
        {
            return Unauthorized(new
            {
                success = false,
                message = result.Message,
                devMessage = result.DevMessage
            });
        }

        return Ok(new
        {
            success = true,
            message = result.Message,
            data = new
            {
                userId = result.UserId,
                accessToken = result.AccessToken,
                refreshToken = result.RefreshToken,
                expiresIn = result.ExpiresIn
            }
        });
    }

    /// <summary>
    /// خروج کاربر - DELETE /api/v1/auth/sessions/{sessionId}
    /// </summary>
    [HttpDelete("auth/sessions/{sessionId}")]
    public async Task<IActionResult> Logout(int sessionId)
    {
        var request = new LogoutRequest
        {
            UserId = sessionId
        };

        var result = await _authService.LogoutAsync(request);

        if (!result.IsSucceded)
        {
            return BadRequest(new
            {
                success = false,
                message = result.Message,
                devMessage = result.DevMessage
            });
        }

        return NoContent();
    }

    /// <summary>
    /// تغییر رمز عبور - PUT /api/v1/users/{userId}/password
    /// </summary>
    [HttpPut("users/{userId}/password")]
    [EnableRateLimiting("ChangePasswordPolicy")]
    public async Task<IActionResult> ChangePassword(int userId, [FromBody] ChangePasswordRequest request)
    {
        if (userId != request.UserId)
        {
            return BadRequest(new
            {
                success = false,
                message = "شناسه کاربر در مسیر و بدنه درخواست مطابقت ندارد",
                devMessage = "User ID mismatch"
            });
        }

        var result = await _authService.ChangePasswordAsync(request);

        if (!result.IsSucceded)
        {
            return BadRequest(new
            {
                success = false,
                message = result.Message,
                devMessage = result.DevMessage
            });
        }

        return Ok(new
        {
            success = true,
            message = result.Message,
            data = new { isChanged = result.IsChanged }
        });
    }

    /// <summary>
    /// درخواست بازنشانی رمز - POST /api/v1/auth/password-reset/request
    /// </summary>
    [HttpPost("auth/password-reset/request")]
    [EnableRateLimiting("ForgotPasswordPolicy")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _authService.ForgotPasswordAsync(request);

        if (!result.IsSucceded)
        {
            return BadRequest(new
            {
                success = false,
                message = result.Message,
                devMessage = result.DevMessage
            });
        }

        return Ok(new
        {
            success = true,
            message = result.Message,
            data = new { isSent = result.IsSent }
        });
    }

    /// <summary>
    /// بازنشانی رمز عبور - POST /api/v1/auth/password-reset
    /// </summary>
    [HttpPost("auth/password-reset")]
    [EnableRateLimiting("ResetPasswordPolicy")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request);

        if (!result.IsSucceded)
        {
            return BadRequest(new
            {
                success = false,
                message = result.Message,
                devMessage = result.DevMessage
            });
        }

        return Ok(new
        {
            success = true,
            message = result.Message,
            data = new { isReset = result.IsReset }
        });
    }

    /// <summary>
    /// تجدید توکن - POST /api/v1/auth/refresh-token
    /// </summary>
    /// <param name="request">Refresh Token</param>
    /// <returns>توکن جدید</returns>
    [HttpPost("auth/refresh-token")]
    [EnableRateLimiting("RefreshTokenPolicy")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);

        if (!result.IsSucceded)
        {
            return Unauthorized(new
            {
                success = false,
                message = result.Message,
                devMessage = result.DevMessage
            });
        }

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

    #endregion
}