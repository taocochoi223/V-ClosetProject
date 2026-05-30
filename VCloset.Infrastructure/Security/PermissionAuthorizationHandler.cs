using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using VCloset.Infrastructure.Data;

namespace VCloset.Infrastructure.Security;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly VClosetVersion30Context _context;

    public PermissionAuthorizationHandler(VClosetVersion30Context context)
    {
        _context = context;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var userIdString = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return; // Không có ID thì đuổi ra

        // 2. Đặc quyền: Nếu là "Super Admin" thì tự động Auto Pass mọi cửa ải!
        var isAdmin = await _context.Set<VCloset.Domain.Entities.AdminProfile>()
            .Include(a => a.PermissionLevelNavigation)
            .AnyAsync(a => a.UserInternalId == userId && a.PermissionLevelNavigation.Name == "super_admin");

        if (isAdmin)
        {
            context.Succeed(requirement);
            return;
        }

        // 3. Nếu là Admin/Moderator thường: Chui vào Database kiểm tra xem có được cấp quyền này không
        var hasPermission = await _context.Set<VCloset.Domain.Entities.AdminPermission>()
            .Include(ap => ap.Permission)
            .AnyAsync(ap => ap.UserInternalId == userId && ap.Permission.Code == requirement.PermissionCode);

        // Nếu có quyền trong Database thì mở cửa cho qua
        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}