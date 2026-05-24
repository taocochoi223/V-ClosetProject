using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCloset.Application.DTOs.Admin.Requests;
using VCloset.Application.DTOs.Admin.Responses;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Domain.Enums;

namespace VCloset.Infrastructure.Services;

public class AdminUserService : IAdminUserService
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminUserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedUsersResponse> GetUsersAsync(
        int page, int pageSize,
        string? search, string? roleFilter,
        bool? isActive, bool? isBanned)
    {
        var allUsers = await _unitOfWork.Users.GetAllAsync();
        var query = allUsers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLowerInvariant();
            query = query.Where(u =>
                u.Email.ToLowerInvariant().Contains(lower) ||
                u.DisplayName.ToLowerInvariant().Contains(lower));
        }

        if (!string.IsNullOrWhiteSpace(roleFilter) &&
            Enum.TryParse<UserRole>(roleFilter, true, out var parsedRole))
        {
            query = query.Where(u => u.Role == parsedRole);
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        var sortedUsers = query.OrderByDescending(u => u.CreatedAt).ToList();

        var allBanLogs = await _unitOfWork.UserBanLogs.GetAllAsync();
        var banLogList = allBanLogs.ToList();

        var summaryList = new List<AdminUserSummaryResponse>();
        foreach (var user in sortedUsers)
        {
            var activeBan = GetActiveBan(banLogList, user.InternalId);
            summaryList.Add(MapToSummary(user, activeBan));
        }

        if (isBanned.HasValue)
        {
            summaryList = summaryList.Where(u => u.IsBanned == isBanned.Value).ToList();
        }

        var totalCount = summaryList.Count;
        var pagedUsers = summaryList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedUsersResponse
        {
            Users = pagedUsers,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AdminUserDetailResponse?> GetUserDetailAsync(Guid targetUserId)
    {
        var user = await _unitOfWork.Users.FindAsync(u => u.Id == targetUserId);
        if (user == null) return null;

        var customerProfile = await _unitOfWork.CustomerProfiles.FindAsync(c => c.UserInternalId == user.InternalId);
        var allBanLogs = await _unitOfWork.UserBanLogs.FindAllAsync(b => b.UserInternalId == user.InternalId);
        var banLogList = allBanLogs.OrderByDescending(b => b.CreatedAt).ToList();

        var activeBan = GetActiveBanFromList(banLogList);

        var banHistory = new List<BanLogResponse>();
        foreach (var log in banLogList)
        {
            var bannedByUser = await _unitOfWork.Users.FindAsync(u => u.InternalId == log.BannedByInternal);
            banHistory.Add(new BanLogResponse
            {
                Id = log.Id,
                BanType = log.BanType,
                Reason = log.Reason,
                BannedUntil = log.BannedUntil,
                IsLifted = log.IsLifted,
                LiftReason = log.LiftReason,
                LiftedAt = log.LiftedAt,
                CreatedAt = log.CreatedAt,
                BannedByDisplayName = bannedByUser?.DisplayName ?? "Unknown"
            });
        }

        var summary = MapToSummary(user, activeBan);

        return new AdminUserDetailResponse
        {
            UserId = summary.UserId,
            Email = summary.Email,
            DisplayName = summary.DisplayName,
            AvatarUrl = summary.AvatarUrl,
            Role = summary.Role,
            IsActive = summary.IsActive,
            IsEmailVerified = summary.IsEmailVerified,
            CreatedAt = summary.CreatedAt,
            IsBanned = summary.IsBanned,
            ActiveBanType = summary.ActiveBanType,
            BannedUntil = summary.BannedUntil,

            PhoneNumber = customerProfile?.PhoneNumber,
            Address = customerProfile?.Address,
            Gender = customerProfile?.Gender,
            Country = customerProfile?.Country,
            HeightCm = customerProfile?.HeightCm,
            WeightKg = customerProfile?.WeightKg,
            DateOfBirth = customerProfile?.DateOfBirth,
            WardrobeItemCount = customerProfile?.WardrobeItemCount ?? 0,
            BanHistory = banHistory
        };
    }

    public async Task BanUserAsync(int adminUserId, Guid targetUserId, BanUserRequest request)
    {
        var targetUser = await _unitOfWork.Users.FindAsync(u => u.Id == targetUserId);
        if (targetUser == null)
            throw new Exception("Không tìm thấy người dùng.");

        if (!targetUser.IsActive)
            throw new Exception("Tài khoản này đã bị vô hiệu hoá.");

        // Kiểm tra xem đã có ban hiệu lực chưa
        var allBanLogs = await _unitOfWork.UserBanLogs.FindAllAsync(b => b.UserInternalId == targetUser.InternalId);
        var activeBan = GetActiveBanFromList(allBanLogs.ToList());
        if (activeBan != null)
            throw new Exception($"Người dùng này đang bị ban '{activeBan.BanType}'. Hãy gỡ ban trước.");

        var validBanTypes = new[] { "chat", "post", "all" };
        if (!validBanTypes.Contains(request.BanType.ToLower()))
            throw new Exception("BanType không hợp lệ. Các giá trị hợp lệ: chat, post, all.");

        var banLog = new UserBanLog
        {
            Id = Guid.NewGuid(),
            UserInternalId = targetUser.InternalId,
            BannedByInternal = adminUserId,
            BanType = request.BanType.ToLower(),
            Reason = request.Reason,
            BannedUntil = request.BannedUntil,
            IsLifted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.UserBanLogs.AddAsync(banLog);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UnbanUserAsync(int adminUserId, Guid targetUserId, string? liftReason)
    {
        var targetUser = await _unitOfWork.Users.FindAsync(u => u.Id == targetUserId);
        if (targetUser == null)
            throw new Exception("Không tìm thấy người dùng.");

        var allBanLogs = await _unitOfWork.UserBanLogs.FindAllAsync(b => b.UserInternalId == targetUser.InternalId);
        var activeBan = GetActiveBanFromList(allBanLogs.ToList());

        if (activeBan == null)
            throw new Exception("Người dùng này hiện không bị ban.");

        activeBan.IsLifted = true;
        activeBan.LiftedByInternal = adminUserId;
        activeBan.LiftedAt = DateTime.UtcNow;
        activeBan.LiftReason = liftReason ?? "Gỡ ban bởi admin";

        _unitOfWork.UserBanLogs.Update(activeBan);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeactivateUserAsync(int adminUserId, Guid targetUserId)
    {
        var adminUser = await _unitOfWork.Users.GetByIdAsync(adminUserId);
        var targetUser = await _unitOfWork.Users.FindAsync(u => u.Id == targetUserId);

        if (adminUser == null) throw new Exception("Không tìm thấy admin.");
        if (targetUser == null) throw new Exception("Không tìm thấy người dùng mục tiêu.");

        if (!targetUser.IsActive)
            throw new Exception("Tài khoản này đã bị vô hiệu hoá rồi.");

        // Chỉ SuperAdmin mới được vô hiệu hoá tài khoản Admin khác
        if (targetUser.Role == UserRole.Admin)
        {
            var adminProfile = await _unitOfWork.AdminProfiles.FindAsync(a => a.UserInternalId == adminUserId);
            if (adminProfile == null)
                throw new Exception("Không tìm thấy AdminProfile.");

            var permissionLevel = await _unitOfWork.PermissionLevels.GetByIdAsync(adminProfile.PermissionLevel);
            if (permissionLevel == null || permissionLevel.Name != "super_admin")
                throw new Exception("Chỉ SuperAdmin mới được phép vô hiệu hoá tài khoản Admin khác.");
        }

        targetUser.IsActive = false;
        targetUser.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(targetUser);
        await _unitOfWork.SaveChangesAsync();
    }

    // ============================
    // Private helpers
    // ============================

    private static UserBanLog? GetActiveBan(List<UserBanLog> allBanLogs, int userInternalId)
    {
        var userBans = allBanLogs.Where(b => b.UserInternalId == userInternalId).ToList();
        return GetActiveBanFromList(userBans);
    }

    private static UserBanLog? GetActiveBanFromList(List<UserBanLog> userBans)
    {
        return userBans.FirstOrDefault(b =>
            !b.IsLifted &&
            (b.BannedUntil == null || b.BannedUntil > DateTime.UtcNow));
    }

    private static AdminUserSummaryResponse MapToSummary(User user, UserBanLog? activeBan)
    {
        return new AdminUserSummaryResponse
        {
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            CreatedAt = user.CreatedAt,
            IsBanned = activeBan != null,
            ActiveBanType = activeBan?.BanType,
            BannedUntil = activeBan?.BannedUntil
        };
    }
}
