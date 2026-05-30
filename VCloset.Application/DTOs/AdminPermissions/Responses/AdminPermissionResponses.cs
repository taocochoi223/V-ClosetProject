using System.Collections.Generic;

namespace VCloset.Application.DTOs.AdminPermissions.Responses;

public class PermissionResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Grp { get; set; } = null!;
}

public class AdminUserPermissionResponse
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string RoleName { get; set; } = null!;
    public List<PermissionResponse> GrantedPermissions { get; set; } = new();
}
