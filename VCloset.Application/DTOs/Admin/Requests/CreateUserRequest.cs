using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using VCloset.Domain.Enums;

namespace VCloset.Application.DTOs.Admin.Requests;

public class CreateUserRequest
{
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Tên hiển thị không được để trống.")]
    public string DisplayName { get; set; } = null!;

    [Required(ErrorMessage = "Vai trò không được để trống.")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserRole Role { get; set; }
}
