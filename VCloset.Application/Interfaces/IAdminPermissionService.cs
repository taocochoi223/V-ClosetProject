using System.Collections.Generic;
using System.Threading.Tasks;
using VCloset.Application.DTOs.AdminPermissions.Requests;
using VCloset.Application.DTOs.AdminPermissions.Responses;

namespace VCloset.Application.Interfaces;

public interface IAdminPermissionService
{
    Task<IEnumerable<PermissionResponse>> GetAllPermissionsAsync();
    Task<AdminUserPermissionResponse> GetUserPermissionsAsync(int userId);
    Task<bool> GrantPermissionsAsync(int userId, UpdatePermissionRequest request, int grantedById);
    Task<bool> RevokePermissionsAsync(int userId, UpdatePermissionRequest request, int revokedById);
    Task<bool> ResetToDefaultPermissionsAsync(int userId, int grantedById);
}
