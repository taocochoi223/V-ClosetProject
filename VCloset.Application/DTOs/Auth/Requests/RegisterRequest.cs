using System.ComponentModel.DataAnnotations;

namespace VCloset.Application.DTOs;

public class RegisterRequest
{
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải chứa ít nhất 6 ký tự.")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Tên hiển thị không được để trống.")]
    [StringLength(100, ErrorMessage = "Tên hiển thị không được vượt quá 100 ký tự.")]
    public string DisplayName { get; set; } = null!;

    [Range(typeof(bool), "true", "true", ErrorMessage = "Bạn phải đồng ý với Điều khoản và Chính sách bảo mật để tiếp tục.")]
    public bool IsAgreedToTerms { get; set; }
}
