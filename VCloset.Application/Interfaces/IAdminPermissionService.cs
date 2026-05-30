using System.Collections.Generic;
using System.Threading.Tasks;
using VCloset.Application.DTOs.AdminPermissions.Requests;
using VCloset.Application.DTOs.AdminPermissions.Responses;

namespace VCloset.Application.Interfaces;

public interface IAdminPermissionService
{
    Task<IEnumerable<PermissionResponse>> GetAllPermissionsAsync();
    Task<AdminUserPermissionResponse> GetUserPermissionsAsync(Guid userId);
    Task<AdminUserPermissionResponse> GetUserPermissionsByInternalIdAsync(int userInternalId);
    Task<bool> GrantPermissionsAsync(Guid userId, UpdatePermissionRequest request, int grantedById);
    Task<bool> RevokePermissionsAsync(Guid userId, UpdatePermissionRequest request, int revokedById);
    Task<bool> ResetToDefaultPermissionsAsync(Guid userId, int grantedById);
}
