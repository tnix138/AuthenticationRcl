using System.Security.Claims;
using AuthenticationRcl.Services;
using AuthenticationRcl.ViewModels.OTP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthenticationRcl.Controllers.Api;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[EnableRateLimiting("GlobalPolicy")]
public class OTPController : ControllerBase
{
    private readonly IOTPService _otpService;

    public OTPController(IOTPService otpService)
    {
        _otpService = otpService;
    }

    // ارسال کد تایید
    [HttpPost("send")]
    [EnableRateLimiting("SendOTPPolicy")]
    public async Task<IActionResult> SendOTP([FromBody] SendOTPRequest request)
    {
        request.UserId = GetCurrentUserId();
        var result = await _otpService.SendOTPAsync(request);

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new
        {
            success = true,
            message = result.Message,
            data = new { otpId = result.OtpId }
        });
    }

    // تایید کد
    [HttpPost("verify")]
    [EnableRateLimiting("VerifyOTPPolicy")]
    public async Task<IActionResult> VerifyOTP([FromBody] VerifyOTPRequest request)
    {
        request.UserId = GetCurrentUserId();
        var result = await _otpService.VerifyOTPAsync(request);

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new
        {
            success = true,
            message = result.Message,
            data = new { isVerified = result.IsVerified }
        });
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? int.Parse(claim.Value) : 0;
    }
}