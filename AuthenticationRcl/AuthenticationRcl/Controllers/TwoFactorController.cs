using System.Security.Claims;
using AuthenticationRcl.Services;
using AuthenticationRcl.ViewModels.TwoFactor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthenticationRcl.Controllers.Api;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[EnableRateLimiting("GlobalPolicy")]
public class TwoFactorController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IOTPService _otpService;

    public TwoFactorController(IAuthService authService, IOTPService otpService)
    {
        _authService = authService;
        _otpService = otpService;
    }

    // دریافت وضعیت 2FA
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var userId = GetCurrentUserId();
        var result = await _authService.GetTwoFactorStatusAsync(userId);

        return Ok(new
        {
            success = true,
            message = result.Message,
            data = new
            {
                isEnabled = result.IsEnabled,
                method = result.Method
            }
        });
    }

    // تنظیم TOTP
    [HttpPost("setup-totp")]
    [EnableRateLimiting("SetupTOTPPolicy")]
    public async Task<IActionResult> SetupTOTP()
    {
        var userId = GetCurrentUserId();
        var result = await _authService.EnableTwoFactorAsync(userId, "TOTP");

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new
        {
            success = true,
            message = result.Message,
            data = new
            {
                secretKey = result.SecretKey,
                qrCodeUrl = result.QRCodeUrl,
                backupCodes = result.BackupCodes
            }
        });
    }

    // تایید TOTP
    [HttpPost("verify-totp")]
    [EnableRateLimiting("VerifyTOTPPolicy")]
    public async Task<IActionResult> VerifyTOTP([FromBody] VerifyTOTPRequest request)
    {
        var userId = GetCurrentUserId();
        var isValid = await _otpService.VerifyTOTPAsync(userId, request.Code);

        if (!isValid)
            return BadRequest(new { success = false, message = "کد تایید نامعتبر است" });

        return Ok(new
        {
            success = true,
            message = "احراز هویت دو مرحله ای با موفقیت فعال شد"
        });
    }

    // غیرفعال سازی 2FA
    [HttpPost("disable")]
    public async Task<IActionResult> DisableTwoFactor()
    {
        var userId = GetCurrentUserId();
        var result = await _authService.DisableTwoFactorAsync(userId);

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new
        {
            success = true,
            message = result.Message
        });
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? int.Parse(claim.Value) : 0;
    }
}