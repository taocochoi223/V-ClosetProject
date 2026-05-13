using System.ComponentModel.DataAnnotations;

namespace VCloset.Application.DTOs;

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ.")]
    public string Email { get; set; } = null!;
}
