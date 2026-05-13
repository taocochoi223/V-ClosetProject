using System.ComponentModel.DataAnnotations;

namespace VCloset.Application.DTOs;

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Token xác thực không được để trống.")]
    public string Token { get; set; } = null!;

    [Required(ErrorMessage = "Mật khẩu mới không được để trống.")]
    [MinLength(6, ErrorMessage = "Mật khẩu mới phải chứa ít nhất 6 ký tự.")]
    public string NewPassword { get; set; } = null!;
}
