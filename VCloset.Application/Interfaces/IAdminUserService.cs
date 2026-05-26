using System;
using System.Threading.Tasks;
using VCloset.Application.DTOs.Admin.Requests;
using VCloset.Application.DTOs.Admin.Responses;

namespace VCloset.Application.Interfaces;

public interface IAdminUserService
{
    Task<AdminUserDetailResponse?> GetUserDetailAsync(Guid targetUserId);
    Task BanUserAsync(int adminUserId, Guid targetUserId, BanUserRequest request);
    Task UnbanUserAsync(int adminUserId, Guid targetUserId, string? liftReason);
    Task DeactivateUserAsync(int adminUserId, Guid targetUserId);
    Task CreateUserWithPermissionsAsync(int creatorAdminId, CreateUserRequest request);
    Task<PagedUsersResponse> GetUsersAsync(int callerAdminId, int page, int pageSize, string? search, string? roleFilter, bool? isActive, bool? isBanned);
}
