using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCloset.Application.DTOs.Admin.Requests;
using VCloset.Application.DTOs.Admin.Responses;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Domain.Enums;
using VCloset.Infrastructure.Data;

namespace VCloset.Infrastructure.Services;

public class AdminUserService : IAdminUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly VClosetVersion30Context _context;
    private readonly IEmailService _emailService;

    public AdminUserService( IUnitOfWork unitOfWork, VClosetVersion30Context context, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _emailService = emailService;
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

    public async Task CreateUserWithPermissionsAsync(int creatorAdminId, CreateUserRequest request)
    {
        // 1. Kiểm tra xem email đã tồn tại hay chưa
        var existingUser = await _unitOfWork.Users.FindAsync(u => u.Email.ToLower() == request.Email.ToLower());
        if (existingUser != null)
        {
            throw new Exception("Email đã được sử dụng bởi một tài khoản khác.");
        }

        // 2. Sinh mật khẩu ngẫu nhiên & băm mật khẩu
        var tempPassword = GenerateRandomPassword(12);
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

        // 3. Tạo tài khoản User mới
        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim(),
            PasswordHash = passwordHash,
            DisplayName = request.DisplayName.Trim(),
            Role = request.Role,
            AuthProvider = AuthProvider.Local,
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(newUser);
        await _unitOfWork.SaveChangesAsync(); // Sinh ra internal_id cho user mới

        // 4. TỰ ĐỘNG map Profile và gán Quyền theo Role
        if (request.Role == UserRole.Admin || request.Role == UserRole.Moderator)
        {
            // TỰ ĐỘNG ĐỊNH NGHĨA LEVEL: Admin = 2, Moderator = 1
            short permLevelId = request.Role == UserRole.Admin ? (short)2 : (short)1;

            // Tạo AdminProfile
            var adminProfile = new AdminProfile
            {
                Id = Guid.NewGuid(),
                UserInternalId = newUser.InternalId,
                PermissionLevel = permLevelId,
                Department = null,
                Notes = $"Tài khoản được tạo bởi Admin ID: {creatorAdminId}",
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.AdminProfiles.AddAsync(adminProfile);

            // Truy vấn và gán tự động các quyền mặc định của Level này từ DB
            var permissionLevel = await _context.PermissionLevels
                .Include(pl => pl.Permissions)
                .FirstOrDefaultAsync(pl => pl.Id == permLevelId);

            if (permissionLevel != null)
            {
                foreach (var perm in permissionLevel.Permissions)
                {
                    var adminPermission = new AdminPermission
                    {
                        UserInternalId = newUser.InternalId,
                        PermissionId = perm.Id,
                        GrantedByInternal = creatorAdminId,
                        GrantedAt = DateTime.UtcNow
                    };
                    await _context.AdminPermissions.AddAsync(adminPermission);
                }
            }
        }
        else if (request.Role == UserRole.BrandPartner)
        {
            var brandProfile = new BrandProfile
            {
                Id = Guid.NewGuid(),
                UserInternalId = newUser.InternalId,
                BrandName = newUser.DisplayName,
                LogoUrl = null,
                WebsiteUrl = null,
                ContactPhone = null,
                TaxCode = null,
                CreditBalance = 0,
                Status = BrandStatus.Verified,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.BrandProfiles.AddAsync(brandProfile);
        }
        else if (request.Role == UserRole.Customer)
        {
            var customerProfile = new CustomerProfile
            {
                Id = Guid.NewGuid(),
                UserInternalId = newUser.InternalId,
                HeightCm = null,
                WeightKg = null,
                DateOfBirth = null,
                PhoneNumber = null,
                Address = null,
                Gender = null,
                Country = null,
                MannequinImageUrl = null,
                MannequinGeneratedAt = null,
                WardrobeItemCount = 0,
                IsChatBanned = false,
                IsPostBanned = false,
                ChatBannedUntil = null,
                PostBannedUntil = null,
                IsOnboardingCompleted = false,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.CustomerProfiles.AddAsync(customerProfile);
        }

        await _unitOfWork.SaveChangesAsync();

        // 5. Gửi email thông báo tài khoản mới chứa mật khẩu ngẫu nhiên
        var mailSent = await _emailService.SendEmailAsync(
            newUser.Email,
            "V-Closet: Thông tin tài khoản mới",
            $"<h3>Chào {newUser.DisplayName},</h3>" +
            $"<p>Tài khoản của bạn đã được tạo thành công bởi quản trị viên hệ thống V-Closet.</p>" +
            $"<p>Dưới đây là thông tin đăng nhập của bạn:</p>" +
            $"<ul>" +
            $"<li><b>Tài khoản:</b> {newUser.Email}</li>" +
            $"<li><b>Mật khẩu tạm thời:</b> <code style='font-size: 15px; color: #d63384; background: #f8f9fa; padding: 3px 6px; border-radius: 4px; font-family: monospace;'>{tempPassword}</code></li>" +
            $"</ul>" +
            $"<p>Vui lòng đăng nhập và thực hiện đổi mật khẩu ngay để đảm bảo an toàn bảo mật.</p>" +
            $"<p>Trân trọng,<br/>Đội ngũ phát triển V-Closet</p>"
        );

        if (!mailSent)
        {
            Console.WriteLine($"[LỖI] Không thể gửi email mật khẩu tạm thời đến địa chỉ {newUser.Email}");
        }
    }

    public async Task<PagedUsersResponse> GetUsersAsync(
                                                        int callerAdminId,
                                                        int page, int pageSize,
                                                        string? search, string? roleFilter,
                                                        bool? isActive, bool? isBanned)
    {
        var caller = await _unitOfWork.Users.GetByIdAsync(callerAdminId);
        if (caller == null)
            throw new Exception("Không tìm thấy thông tin quản trị viên đang gọi API.");

        var callerProfile = await _unitOfWork.AdminProfiles.FindAsync(ap => ap.UserInternalId == callerAdminId);
        int callerRank = 0;
        if (caller.Role == UserRole.Admin)
        {
            callerRank = (callerProfile?.PermissionLevel == 3) ? 3 : 2; 
        }
        else if (caller.Role == UserRole.Moderator)
        {
            callerRank = 1; 
        }
        else
        {
            throw new UnauthorizedAccessException("Bạn không có quyền thực hiện chức năng này.");
        }

        var allUsers = await _unitOfWork.Users.GetAllAsync();
        IEnumerable<User> query = allUsers; 

        var allAdminProfiles = await _context.AdminProfiles.ToListAsync();

        if (callerRank < 3)
        {
            query = query.Where(u =>
            {
                if (u.Role == UserRole.Customer || u.Role == UserRole.BrandPartner)
                    return true;

                var adminProf = allAdminProfiles.FirstOrDefault(ap => ap.UserInternalId == u.InternalId);
                int userRank = (u.Role == UserRole.Admin) ? ((adminProf?.PermissionLevel == 3) ? 3 : 2) : 1;
                return userRank < callerRank;
            });
        }

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
            var summary = MapToSummary(user, activeBan);

            var profile = allAdminProfiles.FirstOrDefault(ap => ap.UserInternalId == user.InternalId);
            if (user.Role == UserRole.Admin && profile?.PermissionLevel == 3)
            {
                summary.Role = "SuperAdmin";
            }

            summaryList.Add(summary);
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


    private static string GenerateRandomPassword(int length = 12)
    {
        const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(validChars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }



}
