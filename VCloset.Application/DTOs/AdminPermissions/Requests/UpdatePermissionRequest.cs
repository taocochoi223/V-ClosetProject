using System.Collections.Generic;

namespace VCloset.Application.DTOs.AdminPermissions.Requests;

public class UpdatePermissionRequest
{
    public List<int> PermissionIds { get; set; } = new();
}
