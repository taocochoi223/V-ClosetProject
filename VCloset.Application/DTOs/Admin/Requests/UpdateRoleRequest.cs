using System.ComponentModel.DataAnnotations;
using VCloset.Domain.Enums;

namespace VCloset.Application.DTOs.Admin.Requests;

public class UpdateRoleRequest
{
    [Required(ErrorMessage = "Vai trò mới không được để trống.")]
    public UserRole NewRole { get; set; }
}
