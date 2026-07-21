using System.Security.Claims;
using AuthenticationRcl.Services;
using AuthenticationRcl.ViewModels.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthenticationRcl.Controllers.Api;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IAuthService _authService;

    public UserController(IAuthService authService)
    {
        _authService = authService;
    }

    // دریافت اطلاعات کاربر جاری
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUserInfo()
    {
        var userId = GetCurrentUserId();
        var result = await _authService.GetUserInfoAsync(userId);

        if (!result.Success)
            return NotFound(new { success = false, message = result.Message });

        return Ok(new
        {
            success = true,
            message = result.Message,
            data = new
            {
                userId = result.UserId,
                username = result.Username,
                email = result.Email,
                phoneNumber = result.PhoneNumber,
                role = result.Role,
                isActive = result.IsActive,
                isEmailConfirmed = result.IsEmailConfirmed,
                isPhoneConfirmed = result.IsPhoneConfirmed,
                twoFactorEnabled = result.TwoFactorEnabled,
                createdAt = result.CreatedAt,
                lastLoginAt = result.LastLoginAt
            }
        });
    }

    // دریافت اطلاعات کاربر با شناسه
    [HttpGet("{userId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUserInfo(int userId)
    {
        var result = await _authService.GetUserInfoAsync(userId);

        if (!result.Success)
            return NotFound(new { success = false, message = result.Message });

        return Ok(new
        {
            success = true,
            message = result.Message,
            data = new
            {
                userId = result.UserId,
                username = result.Username,
                email = result.Email,
                phoneNumber = result.PhoneNumber,
                role = result.Role,
                isActive = result.IsActive,
                isEmailConfirmed = result.IsEmailConfirmed,
                isPhoneConfirmed = result.IsPhoneConfirmed,
                twoFactorEnabled = result.TwoFactorEnabled,
                createdAt = result.CreatedAt,
                lastLoginAt = result.LastLoginAt
            }
        });
    }

    // دریافت اطلاعات کاربر با نام کاربری
    [HttpGet("by-username/{username}")]
    public async Task<IActionResult> GetUserInfoByUsername(string username)
    {
        var result = await _authService.GetUserInfoByUsernameAsync(username);

        if (!result.Success)
            return NotFound(new { success = false, message = result.Message });

        return Ok(new
        {
            success = true,
            message = result.Message,
            data = new
            {
                userId = result.UserId,
                username = result.Username,
                email = result.Email,
                phoneNumber = result.PhoneNumber,
                role = result.Role,
                isActive = result.IsActive,
                isEmailConfirmed = result.IsEmailConfirmed,
                isPhoneConfirmed = result.IsPhoneConfirmed,
                twoFactorEnabled = result.TwoFactorEnabled,
                createdAt = result.CreatedAt,
                lastLoginAt = result.LastLoginAt
            }
        });
    }

    // بروزرسانی پروفایل (ایمیل و شماره موبایل)
    [HttpPut("profile")]
    [EnableRateLimiting("UpdateProfilePolicy")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _authService.UpdateProfileAsync(userId, request);

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new
        {
            success = true,
            message = result.Message,
            data = new
            {
                email = result.Email,
                phoneNumber = result.PhoneNumber,
                isEmailConfirmed = result.IsEmailConfirmed,
                isPhoneConfirmed = result.IsPhoneConfirmed
            }
        });
    }


    // Controllers/Api/UserController.cs - UpdateUser

    [HttpPut("{userId:int}")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("UpdateUserPolicy")]
    public async Task<IActionResult> UpdateUser(int userId, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var result = await _authService.UpdateUserAsync(userId, request);

            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message });

            return Ok(new
            {
                success = true,
                message = result.Message,
                data = new
                {
                    userId = result.UserId,
                    isUpdated = result.IsUpdated
                }
            });
        }
        catch (Exception) 
        {
            return StatusCode(500, new { success = false, message = "خطای سیستمی رخ داده است" });
        }
    }

    // Controllers/Api/UserController.cs - DeleteUser

    [HttpDelete("{userId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        try
        {
            var result = await _authService.DeleteUserAsync(userId);

            if (!result.Success)
                return NotFound(new { success = false, message = result.Message });

            return NoContent();
        }
        catch (Exception) 
        {
            return StatusCode(500, new { success = false, message = "خطای سیستمی رخ داده است" });
        }
    }
    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? int.Parse(claim.Value) : 0;
    }
}

