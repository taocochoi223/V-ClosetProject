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

using Microsoft.EntityFrameworkCore;

public class AdminUserService : IAdminUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly VClosetVersion30Context _context;
    private readonly IEmailService _emailService;
    private readonly INotificationHubService _notificationHubService;

    public AdminUserService( IUnitOfWork unitOfWork, VClosetVersion30Context context, IEmailService emailService, INotificationHubService notificationHubService)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _emailService = emailService;
        _notificationHubService = notificationHubService;
    }

    public async Task<AdminUserDetailResponse?> GetUserDetailAsync(Guid targetUserId)
    {
        var user = await _unitOfWork.Users.FindAsync(u => u.Id == targetUserId);
        if (user == null) return null;

        var customerProfile = await _unitOfWork.CustomerProfiles.FindAsync(c => c.UserInternalId == user.InternalId);
        var adminProfile = await _context.AdminProfiles.FirstOrDefaultAsync(a => a.UserInternalId == user.InternalId);
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

        object? profileDto = null;
        if (user.Role == VCloset.Domain.Enums.UserRole.Admin || user.Role == VCloset.Domain.Enums.UserRole.Moderator)
        {
            if (adminProfile != null)
            {
                profileDto = new Application.DTOs.Admin.Responses.AdminProfileDto
                {
                    PhoneNumber = adminProfile.PhoneNumber,
                    JobTitle = adminProfile.JobTitle,
                    EmployeeCode = adminProfile.EmployeeCode,
                    Department = adminProfile.Department
                };
            }
        }
        else if (user.Role == VCloset.Domain.Enums.UserRole.Customer)
        {
            if (customerProfile != null)
            {
                profileDto = new Application.DTOs.Admin.Responses.CustomerProfileDto
                {
                    PhoneNumber = customerProfile.PhoneNumber,
                    Address = customerProfile.Address,
                    Gender = customerProfile.Gender,
                    Country = customerProfile.Country,
                    HeightCm = customerProfile.HeightCm,
                    WeightKg = customerProfile.WeightKg,
                    DateOfBirth = customerProfile.DateOfBirth,
                    WardrobeItemCount = customerProfile.WardrobeItemCount
                };
            }
        }

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

            Profile = profileDto,
            BanHistory = banHistory
        };
    }

    public async Task BanUserAsync(int adminUserId, Guid targetUserId, BanUserRequest request)
    {
        var targetUser = await _unitOfWork.Users.FindAsync(u => u.Id == targetUserId);
        if (targetUser == null)
            throw new Exception("Không tìm thấy người dùng.");
            
        if (adminUserId == targetUser.InternalId)
            throw new Exception("Bạn không thể thực hiện thao tác này lên chính mình.");

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
            BannedUntil = request.BannedUntil.HasValue ? DateTime.SpecifyKind(request.BannedUntil.Value, DateTimeKind.Utc) : null,
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
            
        if (adminUserId == targetUser.InternalId)
            throw new Exception("Bạn không thể thực hiện thao tác này lên chính mình.");

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
        
        if (adminUserId == targetUser.InternalId)
            throw new Exception("Bạn không thể thực hiện thao tác này lên chính mình.");

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

        // Lưu vết Admin khoá (để phân biệt với tự khoá)
        var banLog = new UserBanLog
        {
            Id = Guid.NewGuid(),
            UserInternalId = targetUser.InternalId,
            BannedByInternal = adminUserId,
            BanType = "deactivate",
            Reason = "Admin vô hiệu hoá tài khoản",
            IsLifted = false,
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.UserBanLogs.AddAsync(banLog);

        await _unitOfWork.SaveChangesAsync();

        await _notificationHubService.SendAdminUserUpdateAlertAsync(new {
            Action = "Deactivate",
            UserId = targetUser.Id,
            DisplayName = targetUser.DisplayName
        });

        // Gửi email thông báo tài khoản bị vô hiệu hóa
        await _emailService.SendAccountDeactivatedEmailAsync(targetUser.Email, targetUser.DisplayName);
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
            InternalId = user.InternalId,
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

        await _notificationHubService.SendAdminUserUpdateAlertAsync(new {
            Action = "Create",
            UserId = newUser.Id,
            DisplayName = newUser.DisplayName
        });

        // 5. Gửi email thông báo tài khoản mới chứa mật khẩu ngẫu nhiên
        var mailSent = await _emailService.SendNewAccountEmailAsync(
            newUser.Email,
            newUser.DisplayName,
            tempPassword
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
                if (u.InternalId == callerAdminId) return true;
                
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

    public async Task GrantPermissionAsync(int adminUserId, Guid targetUserId, string permissionCode)
    {
        // 1. Kiểm tra người gọi có phải là SuperAdmin không
        var callerProfile = await _unitOfWork.AdminProfiles.FindAsync(ap => ap.UserInternalId == adminUserId);
        if (callerProfile == null || callerProfile.PermissionLevel != 3)
        {
            throw new UnauthorizedAccessException("Chỉ có SuperAdmin mới được cấp quyền cho người dùng khác.");
        }

        // 2. Tìm người dùng mục tiêu
        var targetUser = await _unitOfWork.Users.FindAsync(u => u.Id == targetUserId);
        if (targetUser == null)
            throw new Exception("Không tìm thấy người dùng mục tiêu.");

        if (targetUser.Role != UserRole.Admin && targetUser.Role != UserRole.Moderator)
        {
            throw new Exception("Chỉ được cấp quyền cho tài khoản có vai trò Admin hoặc Moderator.");
        }

        // Không được tự cấp cho mình
        if (targetUser.InternalId == adminUserId)
        {
            throw new Exception("Không thể tự cấp quyền cho chính bản thân mình.");
        }

        // 3. Tìm mã quyền
        var permission = await _context.Permissions.FirstOrDefaultAsync(p => p.Code.ToLower() == permissionCode.ToLower());
        if (permission == null)
            throw new Exception($"Không tìm thấy mã quyền '{permissionCode}' trong hệ thống.");

        // 4. Kiểm tra xem đã có quyền này chưa
        var existingPermission = await _unitOfWork.AdminPermissions.FindAsync(ap => 
            ap.UserInternalId == targetUser.InternalId && ap.PermissionId == permission.Id);
        
        if (existingPermission != null)
        {
            throw new Exception("Người dùng này đã được cấp quyền này trước đó.");
        }

        // 5. Cấp quyền mới
        var newPermission = new AdminPermission
        {
            UserInternalId = targetUser.InternalId,
            PermissionId = permission.Id,
            GrantedByInternal = adminUserId,
            GrantedAt = DateTime.UtcNow
        };

        await _unitOfWork.AdminPermissions.AddAsync(newPermission);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task RevokePermissionAsync(int adminUserId, Guid targetUserId, string permissionCode)
    {
        // 1. Kiểm tra người gọi có phải là SuperAdmin không
        var callerProfile = await _unitOfWork.AdminProfiles.FindAsync(ap => ap.UserInternalId == adminUserId);
        if (callerProfile == null || callerProfile.PermissionLevel != 3)
        {
            throw new UnauthorizedAccessException("Chỉ có SuperAdmin mới được thu hồi quyền của người dùng khác.");
        }

        // 2. Tìm người dùng mục tiêu
        var targetUser = await _unitOfWork.Users.FindAsync(u => u.Id == targetUserId);
        if (targetUser == null)
            throw new Exception("Không tìm thấy người dùng mục tiêu.");

        // Không được tự thu hồi của mình
        if (targetUser.InternalId == adminUserId)
        {
            throw new Exception("Không thể tự thu hồi quyền của chính bản thân mình.");
        }

        // 3. Tìm mã quyền
        var permission = await _context.Permissions.FirstOrDefaultAsync(p => p.Code.ToLower() == permissionCode.ToLower());
        if (permission == null)
            throw new Exception($"Không tìm thấy mã quyền '{permissionCode}' trong hệ thống.");

        // 4. Tìm bản ghi quyền để xóa
        var existingPermission = await _unitOfWork.AdminPermissions.FindAsync(ap => 
            ap.UserInternalId == targetUser.InternalId && ap.PermissionId == permission.Id);

        if (existingPermission == null)
        {
            throw new Exception("Người dùng này hiện không có quyền này để thu hồi.");
        }

        _unitOfWork.AdminPermissions.Delete(existingPermission);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ReactivateUserAsync(int adminUserId, Guid targetUserId)
    {
        var targetUser = await _unitOfWork.Users.FindAsync(u => u.Id == targetUserId);
        if (targetUser == null)
            throw new Exception("Không tìm thấy người dùng mục tiêu.");
            
        if (adminUserId == targetUser.InternalId)
            throw new Exception("Bạn không thể thực hiện thao tác này lên chính mình.");

        if (targetUser.IsActive)
            throw new Exception("Tài khoản người dùng này vẫn đang hoạt động.");

        // Chỉ SuperAdmin mới được phép kích hoạt lại Admin khác
        if (targetUser.Role == UserRole.Admin)
        {
            var callerProfile = await _unitOfWork.AdminProfiles.FindAsync(ap => ap.UserInternalId == adminUserId);
            if (callerProfile == null || callerProfile.PermissionLevel != 3)
            {
                throw new UnauthorizedAccessException("Chỉ có SuperAdmin mới được kích hoạt lại tài khoản Admin khác.");
            }
        }

        targetUser.IsActive = true;
        targetUser.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(targetUser);

        // Gỡ vết Admin khoá (nếu có)
        var activeDeactivateLogs = await _unitOfWork.UserBanLogs.FindAllAsync(b => b.UserInternalId == targetUser.InternalId && b.BanType == "deactivate" && !b.IsLifted);
        foreach(var log in activeDeactivateLogs)
        {
            log.IsLifted = true;
            log.LiftedByInternal = adminUserId;
            log.LiftedAt = DateTime.UtcNow;
            log.LiftReason = "Admin kích hoạt lại tài khoản";
            _unitOfWork.UserBanLogs.Update(log);
        }

        await _unitOfWork.SaveChangesAsync();

        await _notificationHubService.SendAdminUserUpdateAlertAsync(new {
            Action = "Reactivate",
            UserId = targetUser.Id,
            DisplayName = targetUser.DisplayName
        });

        // Gửi email thông báo tài khoản được kích hoạt lại
        await _emailService.SendAccountReactivatedEmailAsync(targetUser.Email, targetUser.DisplayName);
    }

    public async Task ResetPermissionsToDefaultAsync(int adminUserId, Guid targetUserId)
    {
        // 1. Kiểm tra người gọi có phải là SuperAdmin không
        var callerProfile = await _unitOfWork.AdminProfiles.FindAsync(ap => ap.UserInternalId == adminUserId);
        if (callerProfile == null || callerProfile.PermissionLevel != 3)
        {
            throw new UnauthorizedAccessException("Chỉ có SuperAdmin mới được khôi phục quyền mặc định.");
        }

        // 2. Tìm người dùng mục tiêu
        var targetUser = await _unitOfWork.Users.FindAsync(u => u.Id == targetUserId);
        if (targetUser == null)
            throw new Exception("Không tìm thấy người dùng mục tiêu.");

        if (targetUser.Role != UserRole.Admin && targetUser.Role != UserRole.Moderator)
        {
            throw new Exception("Chỉ hỗ trợ khôi phục quyền cho tài khoản có vai trò Admin hoặc Moderator.");
        }

        // Không được tự reset cho mình
        if (targetUser.InternalId == adminUserId)
        {
            throw new Exception("Không thể tự khôi phục quyền mặc định cho chính bản thân mình.");
        }

        // 3. Xoá toàn bộ quyền hiện tại
        var userPermissions = await _unitOfWork.AdminPermissions.FindAllAsync(ap => ap.UserInternalId == targetUser.InternalId);
        foreach (var userPerm in userPermissions)
        {
            _unitOfWork.AdminPermissions.Delete(userPerm);
        }

        // 4. Lấy danh sách quyền mặc định của vai trò đó từ DB
        short defaultLevelId = targetUser.Role == UserRole.Admin ? (short)2 : (short)1;
        var permissionLevel = await _context.PermissionLevels
            .Include(pl => pl.Permissions)
            .FirstOrDefaultAsync(pl => pl.Id == defaultLevelId);

        if (permissionLevel != null)
        {
            foreach (var perm in permissionLevel.Permissions)
            {
                var adminPermission = new AdminPermission
                {
                    UserInternalId = targetUser.InternalId,
                    PermissionId = perm.Id,
                    GrantedByInternal = adminUserId,
                    GrantedAt = DateTime.UtcNow
                };
                await _unitOfWork.AdminPermissions.AddAsync(adminPermission);
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateUserRoleAsync(int adminUserId, Guid targetUserId, UserRole newRole)
    {
        // 1. Kiểm tra người gọi có phải là SuperAdmin không
        var callerProfile = await _unitOfWork.AdminProfiles.FindAsync(ap => ap.UserInternalId == adminUserId);
        if (callerProfile == null || callerProfile.PermissionLevel != 3)
        {
            throw new UnauthorizedAccessException("Chỉ có SuperAdmin mới được thay đổi vai trò của người dùng.");
        }

        // 2. Tìm người dùng mục tiêu
        var targetUser = await _unitOfWork.Users.FindAsync(u => u.Id == targetUserId);
        if (targetUser == null)
            throw new Exception("Không tìm thấy người dùng mục tiêu.");

        // Không được tự thay đổi role của mình
        if (targetUser.InternalId == adminUserId)
        {
            throw new Exception("Không thể tự thay đổi vai trò của chính bản thân mình.");
        }

        var oldRole = targetUser.Role;
        if (oldRole == newRole)
        {
            return; // Không thay đổi gì
        }

        // 3. Xử lý Profile cũ (xoá hoặc cập nhật)
        if (oldRole == UserRole.Admin || oldRole == UserRole.Moderator)
        {
            if (newRole != UserRole.Admin && newRole != UserRole.Moderator)
            {
                var adminProfile = await _unitOfWork.AdminProfiles.FindAsync(ap => ap.UserInternalId == targetUser.InternalId);
                if (adminProfile != null)
                {
                    _unitOfWork.AdminProfiles.Delete(adminProfile);
                }

                var permissions = await _unitOfWork.AdminPermissions.FindAllAsync(ap => ap.UserInternalId == targetUser.InternalId);
                foreach (var p in permissions)
                {
                    _unitOfWork.AdminPermissions.Delete(p);
                }
            }
        }
        else if (oldRole == UserRole.Customer)
        {
            var customerProfile = await _unitOfWork.CustomerProfiles.FindAsync(cp => cp.UserInternalId == targetUser.InternalId);
            if (customerProfile != null)
            {
                _unitOfWork.CustomerProfiles.Delete(customerProfile);
            }
        }
        else if (oldRole == UserRole.BrandPartner)
        {
            var brandProfile = await _unitOfWork.BrandProfiles.FindAsync(bp => bp.UserInternalId == targetUser.InternalId);
            if (brandProfile != null)
            {
                _unitOfWork.BrandProfiles.Delete(brandProfile);
            }
        }

        // 4. Khởi tạo Profile mới và gán quyền tương ứng
        if (newRole == UserRole.Admin || newRole == UserRole.Moderator)
        {
            short permLevelId = newRole == UserRole.Admin ? (short)2 : (short)1;

            var adminProfile = await _unitOfWork.AdminProfiles.FindAsync(ap => ap.UserInternalId == targetUser.InternalId);
            if (adminProfile == null)
            {
                adminProfile = new AdminProfile
                {
                    Id = Guid.NewGuid(),
                    UserInternalId = targetUser.InternalId,
                    PermissionLevel = permLevelId,
                    Department = null,
                    EmployeeCode = $"EMP-{targetUser.InternalId:D4}",
                    Notes = $"Thay đổi vai trò bởi SuperAdmin ID: {adminUserId}",
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.AdminProfiles.AddAsync(adminProfile);
            }
            else
            {
                adminProfile.PermissionLevel = permLevelId;
                _unitOfWork.AdminProfiles.Update(adminProfile);
            }

            // Reset và gán quyền mặc định của vai trò mới
            var existingPermissions = await _unitOfWork.AdminPermissions.FindAllAsync(ap => ap.UserInternalId == targetUser.InternalId);
            foreach (var p in existingPermissions)
            {
                _unitOfWork.AdminPermissions.Delete(p);
            }

            var permissionLevel = await _context.PermissionLevels
                .Include(pl => pl.Permissions)
                .FirstOrDefaultAsync(pl => pl.Id == permLevelId);

            if (permissionLevel != null)
            {
                foreach (var perm in permissionLevel.Permissions)
                {
                    var adminPermission = new AdminPermission
                    {
                        UserInternalId = targetUser.InternalId,
                        PermissionId = perm.Id,
                        GrantedByInternal = adminUserId,
                        GrantedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.AdminPermissions.AddAsync(adminPermission);
                }
            }
        }
        else if (newRole == UserRole.Customer)
        {
            var customerProfile = await _unitOfWork.CustomerProfiles.FindAsync(cp => cp.UserInternalId == targetUser.InternalId);
            if (customerProfile == null)
            {
                customerProfile = new CustomerProfile
                {
                    Id = Guid.NewGuid(),
                    UserInternalId = targetUser.InternalId,
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
        }
        else if (newRole == UserRole.BrandPartner)
        {
            var brandProfile = await _unitOfWork.BrandProfiles.FindAsync(bp => bp.UserInternalId == targetUser.InternalId);
            if (brandProfile == null)
            {
                brandProfile = new BrandProfile
                {
                    Id = Guid.NewGuid(),
                    UserInternalId = targetUser.InternalId,
                    BrandName = targetUser.DisplayName,
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
        }

        // 5. Cập nhật role trong thực thể User
        targetUser.Role = newRole;
        targetUser.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(targetUser);

        await _unitOfWork.SaveChangesAsync();
    }
    public async Task UpdateAdminInternalInfoAsync(int adminUserId, Guid targetUserId, UpdateAdminInternalInfoRequest request)
    {
        // 1. Kiểm tra người gọi có phải là SuperAdmin không
        var callerProfile = await _unitOfWork.AdminProfiles.FindAsync(ap => ap.UserInternalId == adminUserId);
        if (callerProfile == null || callerProfile.PermissionLevel != 3) // 3 is SuperAdmin usually, checking PermissionLevel
        {
            throw new UnauthorizedAccessException("Chỉ có SuperAdmin mới được điều chuyển/sửa thông tin nội bộ của người dùng khác.");
        }

        // 2. Tìm người dùng mục tiêu
        var targetUser = await _unitOfWork.Users.FindAsync(u => u.Id == targetUserId);
        if (targetUser == null)
            throw new Exception("Không tìm thấy người dùng mục tiêu.");

        if (targetUser.Role != UserRole.Admin && targetUser.Role != UserRole.Moderator)
        {
            throw new Exception("Chỉ được cập nhật thông tin nội bộ cho tài khoản có vai trò Admin hoặc Moderator.");
        }

        var targetProfile = await _unitOfWork.AdminProfiles.FindAsync(ap => ap.UserInternalId == targetUser.InternalId);
        if (targetProfile == null)
        {
            throw new Exception("Người dùng này không có AdminProfile.");
        }

        if (request.Department != null) targetProfile.Department = request.Department;
        if (request.JobTitle != null) targetProfile.JobTitle = request.JobTitle;
        if (request.EmployeeCode != null) targetProfile.EmployeeCode = request.EmployeeCode;
        if (request.Notes != null) targetProfile.Notes = request.Notes;

        _unitOfWork.AdminProfiles.Update(targetProfile);
        await _unitOfWork.SaveChangesAsync();
    }

    private static string GenerateRandomPassword(int length = 12)
    {
        const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(validChars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
