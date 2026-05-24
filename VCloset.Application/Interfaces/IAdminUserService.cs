using System;
using System.Threading.Tasks;
using VCloset.Application.DTOs.Admin.Requests;
using VCloset.Application.DTOs.Admin.Responses;

namespace VCloset.Application.Interfaces;

public interface IAdminUserService
{

    Task<PagedUsersResponse> GetUsersAsync(int page, int pageSize, string? search, string? roleFilter, bool? isActive, bool? isBanned);
    Task<AdminUserDetailResponse?> GetUserDetailAsync(Guid targetUserId);
    Task BanUserAsync(int adminUserId, Guid targetUserId, BanUserRequest request);
    Task UnbanUserAsync(int adminUserId, Guid targetUserId, string? liftReason);
    Task DeactivateUserAsync(int adminUserId, Guid targetUserId);
}
