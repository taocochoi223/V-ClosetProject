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
        return Ok("Nếu email tồn tại trên hệ thống, một liên kết đặt lại mật khẩu đã được gửi đi.");
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request);
        if (!result) return BadRequest("Mã xác thực không hợp lệ hoặc đã hết hạn.");
        return Ok("Đặt lại mật khẩu thành công! Bạn có thể đăng nhập bằng mật khẩu mới.");
    }

    [HttpGet("reset-password-form")]
    public IActionResult GetResetPasswordForm([FromQuery] string token)
    {
        var html = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <title>V-Closet: Đặt lại mật khẩu</title>
                <meta charset='utf-8'>
                <style>
                    body {{ font-family: Arial, sans-serif; background-color: #f4f6f9; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; }}
                    .card {{ background: white; padding: 30px; border-radius: 10px; box-shadow: 0 4px 15px rgba(0,0,0,0.1); width: 350px; text-align: center; }}
                    h2 {{ color: #4F46E5; margin-bottom: 20px; }}
                    input {{ width: 90%; padding: 10px; margin: 10px 0; border: 1px solid #ccc; border-radius: 5px; font-size: 16px; }}
                    button {{ background: #4F46E5; color: white; border: none; padding: 12px 20px; width: 95%; border-radius: 5px; font-size: 16px; font-weight: bold; cursor: pointer; margin-top: 15px; }}
                    button:hover {{ background: #4338CA; }}
                </style>
            </head>
            <body>
                <div class='card'>
                    <h2>V-Closet</h2>
                    <p>Vui lòng nhập mật khẩu mới của bạn:</p>
                    <form action='/api/auth/reset-password-submit' method='POST'>
                        <input type='hidden' name='token' value='{token}' />
                        <input type='password' name='newPassword' placeholder='Mật khẩu mới (ít nhất 6 ký tự)' required minlength='6' />
                        <button type='submit'>Xác nhận đổi mật khẩu</button>
                    </form>
                </div>
            </body>
            </html>";

        return Content(html, "text/html", System.Text.Encoding.UTF8);
    }

    [HttpPost("reset-password-submit")]
    public async Task<IActionResult> SubmitResetPassword([FromForm] string token, [FromForm] string newPassword)
    {
        var request = new ResetPasswordRequest { Token = token, NewPassword = newPassword };
        var result = await _authService.ResetPasswordAsync(request);

        string message;
        if (result)
        {
            message = "<h2 style='color: #10B981;'>Thành công!</h2><p>Mật khẩu của bạn đã được đặt lại thành công. Bây giờ bạn đã có thể đăng nhập bằng mật khẩu mới!</p>";
        }
        else
        {
            message = "<h2 style='color: #EF4444;'>Lỗi!</h2><p>Mã xác thực không hợp lệ hoặc đã hết hạn 15 phút. Vui lòng yêu cầu lại mã mới.</p>";
        }

        var htmlResult = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Kết quả đặt lại mật khẩu</title>
                <meta charset='utf-8'>
                <style>
                    body {{ font-family: Arial, sans-serif; background-color: #f4f6f9; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; }}
                    .card {{ background: white; padding: 40px; border-radius: 10px; box-shadow: 0 4px 15px rgba(0,0,0,0.1); width: 400px; text-align: center; }}
                </style>
            </head>
            <body>
                <div class='card'>
                    {message}
                </div>
            </body>
            </html>";

        return Content(htmlResult, "text/html", System.Text.Encoding.UTF8);
    }
}
