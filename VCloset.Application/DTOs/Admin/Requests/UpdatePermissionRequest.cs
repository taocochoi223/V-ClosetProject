using System.ComponentModel.DataAnnotations;

namespace VCloset.Application.DTOs.Admin.Requests;

public class UpdatePermissionRequest
{
    [Required(ErrorMessage = "Mã quyền (Permission Code) không được để trống.")]
    public string PermissionCode { get; set; } = null!;
}
