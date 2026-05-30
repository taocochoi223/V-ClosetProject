using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VCloset.Application.DTOs.AdminPermissions.Requests;
using VCloset.Application.DTOs.AdminPermissions.Responses;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Infrastructure.Data;

namespace VCloset.Infrastructure.Services;

public class AdminPermissionService : IAdminPermissionService
{
    private readonly VClosetVersion30Context _context;

    public AdminPermissionService(VClosetVersion30Context context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PermissionResponse>> GetAllPermissionsAsync()
    {
        return await _context.Permissions
            .OrderBy(p => p.Grp)
            .ThenBy(p => p.Code)
            .Select(p => new PermissionResponse
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                Grp = p.Grp
            })
            .ToListAsync();
    }

    public async Task<AdminUserPermissionResponse> GetUserPermissionsAsync(Guid userId)
    {
        var adminProfile = await _context.AdminProfiles
            .Include(a => a.UserInternal)
            .Include(a => a.PermissionLevelNavigation)
            .FirstOrDefaultAsync(a => a.UserInternal.Id == userId);

        if (adminProfile == null)
            throw new Exception("Không tìm thấy profile admin của người dùng này.");

        var permissions = await _context.AdminPermissions
            .Include(ap => ap.Permission)
            .Where(ap => ap.UserInternalId == adminProfile.UserInternalId)
            .Select(ap => new PermissionResponse
            {
                Id = ap.Permission.Id,
                Code = ap.Permission.Code,
                Name = ap.Permission.Name,
                Description = ap.Permission.Description,
                Grp = ap.Permission.Grp
            })
            .ToListAsync();

        return new AdminUserPermissionResponse
        {
            UserId = userId,
            DisplayName = adminProfile.UserInternal.DisplayName,
            Email = adminProfile.UserInternal.Email,
            RoleName = adminProfile.PermissionLevelNavigation.Name,
            GrantedPermissions = permissions
        };
    }

    public async Task<AdminUserPermissionResponse> GetUserPermissionsByInternalIdAsync(int userInternalId)
    {
        var adminProfile = await _context.AdminProfiles
            .Include(a => a.UserInternal)
            .Include(a => a.PermissionLevelNavigation)
            .FirstOrDefaultAsync(a => a.UserInternalId == userInternalId);

        if (adminProfile == null)
            throw new Exception("Không tìm thấy profile admin của người dùng này.");

        var permissions = await _context.AdminPermissions
            .Include(ap => ap.Permission)
            .Where(ap => ap.UserInternalId == userInternalId)
            .Select(ap => new PermissionResponse
            {
                Id = ap.Permission.Id,
                Code = ap.Permission.Code,
                Name = ap.Permission.Name,
                Description = ap.Permission.Description,
                Grp = ap.Permission.Grp
            })
            .ToListAsync();

        return new AdminUserPermissionResponse
        {
            UserId = adminProfile.UserInternal.Id, // return Guid
            DisplayName = adminProfile.UserInternal.DisplayName,
            Email = adminProfile.UserInternal.Email,
            RoleName = adminProfile.PermissionLevelNavigation.Name,
            GrantedPermissions = permissions
        };
    }

    public async Task<bool> GrantPermissionsAsync(Guid userId, UpdatePermissionRequest request, int grantedById)
    {
        var adminProfile = await _context.AdminProfiles.Include(a => a.UserInternal).FirstOrDefaultAsync(a => a.UserInternal.Id == userId);
        if (adminProfile == null) throw new Exception("Không tìm thấy Admin Profile.");

        await EnsureHasRoleHierarchyPrivilegeAsync(grantedById, adminProfile.UserInternalId);

        var currentPermissionIds = await _context.AdminPermissions
            .Where(ap => ap.UserInternalId == adminProfile.UserInternalId)
            .Select(ap => ap.PermissionId)
            .ToListAsync();

        var newPermissions = request.PermissionIds.Except(currentPermissionIds).ToList();

        foreach (var pId in newPermissions)
        {
            _context.AdminPermissions.Add(new AdminPermission
            {
                UserInternalId = adminProfile.UserInternalId,
                PermissionId = pId,
                GrantedByInternal = grantedById,
                GrantedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RevokePermissionsAsync(Guid userId, UpdatePermissionRequest request, int revokedById)
    {
        var adminProfile = await _context.AdminProfiles.Include(a => a.UserInternal).FirstOrDefaultAsync(a => a.UserInternal.Id == userId);
        if (adminProfile == null) throw new Exception("Không tìm thấy Admin Profile.");

        await EnsureHasRoleHierarchyPrivilegeAsync(revokedById, adminProfile.UserInternalId);

        var toRevoke = await _context.AdminPermissions
            .Where(ap => ap.UserInternalId == adminProfile.UserInternalId && request.PermissionIds.Contains(ap.PermissionId))
            .ToListAsync();

        if (toRevoke.Any())
        {
            _context.AdminPermissions.RemoveRange(toRevoke);
            await _context.SaveChangesAsync();
        }

        return true;
    }

    public async Task<bool> ResetToDefaultPermissionsAsync(Guid userId, int grantedById)
    {
        var adminProfile = await _context.AdminProfiles
            .Include(a => a.UserInternal)
            .FirstOrDefaultAsync(a => a.UserInternal.Id == userId);

        if (adminProfile == null) throw new Exception("Không tìm thấy Admin Profile.");

        await EnsureHasRoleHierarchyPrivilegeAsync(grantedById, adminProfile.UserInternalId);

        if (adminProfile == null) throw new Exception("Không tìm thấy Admin Profile.");

        // Lấy quyền mặc định của Role
        var defaultPermissions = await _context.Set<Dictionary<string, object>>("PermissionLevelDefault")
            .Where(p => (short)p["PermissionLevelId"] == adminProfile.PermissionLevel)
            .Select(p => (int)p["PermissionId"])
            .ToListAsync();

        // Xóa hết quyền cũ
        var oldPermissions = await _context.AdminPermissions
            .Where(ap => ap.UserInternalId == adminProfile.UserInternalId)
            .ToListAsync();
        
        _context.AdminPermissions.RemoveRange(oldPermissions);

        // Add lại quyền mặc định
        foreach (var pId in defaultPermissions)
        {
            _context.AdminPermissions.Add(new AdminPermission
            {
                UserInternalId = adminProfile.UserInternalId,
                PermissionId = pId,
                GrantedByInternal = grantedById,
                GrantedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    private async Task EnsureHasRoleHierarchyPrivilegeAsync(int currentUserId, int targetUserId)
    {
        var currentUserProfile = await _context.AdminProfiles.FirstOrDefaultAsync(a => a.UserInternalId == currentUserId);
        var targetUserProfile = await _context.AdminProfiles.FirstOrDefaultAsync(a => a.UserInternalId == targetUserId);

        if (currentUserProfile == null || targetUserProfile == null)
            throw new Exception("Không tìm thấy Admin Profile.");

        // Super Admin (Level 3) có toàn quyền
        if (currentUserProfile.PermissionLevel == 3) return;

        // Admin (Level 2) hoặc Moderator (Level 1) không được phép chỉnh sửa người có cấp bậc ngang hoặc cao hơn mình
        // Kể cả tự chỉnh sửa bản thân cũng không được (tránh việc Admin tự cấp thêm quyền vượt cấp cho mình)
        if (currentUserProfile.PermissionLevel <= targetUserProfile.PermissionLevel)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền thay đổi phân quyền của người dùng có cấp bậc ngang bằng hoặc cao hơn.");
        }
    }
}
