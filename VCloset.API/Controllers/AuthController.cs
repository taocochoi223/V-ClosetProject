using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VCloset.Application.DTOs;
using VCloset.Application.DTOs.Auth.Requests;
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

    /// <summary>
    /// API Đăng ký tài khoản mới (Mặc định vai trò Customer).
    /// Hệ thống sẽ gửi một mã kích hoạt OTP tới Email đăng ký.
    /// </summary>
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

    /// <summary>
    /// API Xác thực mã OTP để kích hoạt tài khoản.
    /// Trả về Access Token, Refresh Token và thông tin User sau khi xác thực thành công.
    /// </summary>
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        var response = await _authService.VerifyOtpAsync(request);
        if (response == null) return BadRequest("Mã OTP không chính xác hoặc đã hết hạn.");
        return Ok(response);
    }

    /// <summary>
    /// API Đăng nhập tài khoản cục bộ (Local Login bằng Email và Mật khẩu).
    /// Trả về Access Token, Refresh Token, Vai trò (Role) và thông tin cơ bản của User.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return Unauthorized(new
            {
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// API Yêu cầu quên mật khẩu.
    /// Hệ thống sẽ gửi một mã OTP khôi phục mật khẩu tới Email được yêu cầu.
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _authService.ForgotPasswordAsync(request);
        return Ok("Nếu email tồn tại trên hệ thống, một mã xác thực OTP đã được gửi đi.");
    }

    /// <summary>
    /// API Đặt lại mật khẩu mới bằng mã OTP đã nhận qua Email.
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request);
        if (!result) return BadRequest("Mã xác thực OTP không hợp lệ hoặc đã hết hạn.");
        return Ok("Đặt lại mật khẩu thành công! Bạn có thể đăng nhập bằng mật khẩu mới.");
    }

    /// <summary>
    /// API Đăng nhập / Đăng ký nhanh bằng Google Account (Sử dụng Google ID Token).
    /// Hệ thống tự động tạo tài khoản Customer mới nếu Email Google chưa đăng ký trên hệ thống.
    /// </summary>
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

    /// <summary>
    /// API Yêu cầu gửi lại mã kích hoạt OTP tới Email đăng ký.
    /// </summary>
    [HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request)
    {
        try
        {
            await _authService.ResendOtpAsync(request);
            return Ok("Mã OTP mới đã được gửi đến Email của bạn.");
        }
        catch (System.Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// API Đổi mật khẩu của người dùng (Yêu cầu đăng nhập).
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            await _authService.ChangePasswordAsync(userId, request);
            return Ok("Đổi mật khẩu thành công!");
        }
        catch (System.Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// API Làm mới Access Token bằng Refresh Token (Gia hạn phiên đăng nhập không cần nhập lại mật khẩu).
    /// </summary>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(request);
            return Ok(response);
        }
        catch (System.Exception ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    /// <summary>
    /// API Đăng xuất (Yêu cầu đăng nhập).
    /// Hệ thống sẽ thu hồi và xóa Refresh Token hiện tại ra khỏi cơ sở dữ liệu.
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        try
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            await _authService.LogoutAsync(userId, request.RefreshToken);
            return Ok("Đăng xuất thành công!");
        }
        catch (System.Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
