using System.ComponentModel.DataAnnotations;

namespace VCloset.Application.DTOs;

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Mã xác thực OTP không được để trống.")]
    public string OtpCode { get; set; } = null!;

    [Required(ErrorMessage = "Mật khẩu mới không được để trống.")]
    [MinLength(6, ErrorMessage = "Mật khẩu mới phải chứa ít nhất 6 ký tự.")]
    public string NewPassword { get; set; } = null!;
}
