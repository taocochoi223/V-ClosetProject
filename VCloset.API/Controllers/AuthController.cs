using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VCloset.Application.DTOs;
using VCloset.Application.Interfaces;

namespace VCloset.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            await _authService.RegisterAsync(request);
            return Ok("Đăng ký thành công! Vui lòng kiểm tra Email để lấy mã kích hoạt OTP.");
        }
        catch (System.InvalidOperationException ex) when (ex.Message == "EMAIL_ALREADY_EXISTS")
        {
            return Conflict("Đăng ký thất bại. Email này đã được sử dụng và xác thực trên hệ thống.");
        }
        catch (System.InvalidOperationException ex) when (ex.Message == "EMAIL_SEND_FAILED")
        {
            return StatusCode(500, "Đăng ký thất bại. Hệ thống không thể gửi Email chứa mã OTP. Vui lòng kiểm tra lại cấu hình Email (Gmail SMTP hoặc Resend API Key) trong file .env và nhớ KHỞI ĐỘNG LẠI dự án.");
        }
        catch (System.Exception ex)
        {
            return BadRequest($"Đăng ký thất bại: {ex.Message}");
        }
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        var response = await _authService.VerifyOtpAsync(request);
        if (response == null) return BadRequest("Mã OTP không chính xác hoặc đã hết hạn.");
        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        if (response == null) return Unauthorized("Email hoặc Mật khẩu không chính xác, hoặc tài khoản chưa được kích hoạt.");
        return Ok(response);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _authService.ForgotPasswordAsync(request);
        return Ok("Nếu email tồn tại trên hệ thống, một mã xác thực OTP đã được gửi đi.");
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request);
        if (!result) return BadRequest("Mã xác thực OTP không hợp lệ hoặc đã hết hạn.");
        return Ok("Đặt lại mật khẩu thành công! Bạn có thể đăng nhập bằng mật khẩu mới.");
    }

    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await _authService.GoogleLoginAsync(request);

        if (response == null)
            return Unauthorized(new { message = "Xác thực Google thất bại hoặc Token không hợp lệ." });
        return Ok(new { message = "Đăng nhập Google thành công", data = response });
    }
}
