using System.ComponentModel.DataAnnotations;

namespace VCloset.Application.DTOs;

public class VerifyOtpRequest
{
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Mã OTP không được để trống.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã OTP phải chứa đúng 6 chữ số.")]
    public string OtpCode { get; set; } = null!;
}
