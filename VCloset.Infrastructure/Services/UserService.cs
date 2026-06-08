using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using VCloset.Application.DTOs;
using VCloset.Application.DTOs.Users.Responses;
using VCloset.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace VCloset.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageService _storageService;

    public UserService(IUnitOfWork unitOfWork, IStorageService storageService)
    {
        _unitOfWork = unitOfWork;
        _storageService = storageService;
    }

    public async Task<UserProfileResponse?> GetMyProfileAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) return null;

        var response = new UserProfileResponse
        {
            UserId = user.InternalId,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role.ToString(),
        };

        if (user.Role == VCloset.Domain.Enums.UserRole.Customer)
        {
            var profile = await _unitOfWork.CustomerProfiles.FindAsync(c => c.UserInternalId == userId);
            response.HeightCm = profile?.HeightCm;
            response.WeightKg = profile?.WeightKg;
            response.DateOfBirth = profile?.DateOfBirth;
            response.PhoneNumber = profile?.PhoneNumber;
            response.Address = profile?.Address;
            response.Gender = profile?.Gender;
            response.Country = profile?.Country;
            response.BodyShape = profile?.BodyShape?.ToString();
            response.MannequinImageUrl = profile?.MannequinImageUrl;
            response.WardrobeItemCount = profile?.WardrobeItemCount ?? 0;
            response.IsOnboardingCompleted = profile?.IsOnboardingCompleted ?? false;
            response.Lifestyle = profile?.Lifestyle;
            response.EyeColor = profile?.EyeColor;
            response.Hair = profile?.Hair;
        }
        else
        {
            var adminProfile = await _unitOfWork.AdminProfiles.FindAsync(a => a.UserInternalId == userId);
            response.PhoneNumber = adminProfile?.PhoneNumber;
            response.IsOnboardingCompleted = true;
        }

        return response;
    }

    public async Task<bool> UpdateMyProfileAsync(int userId, UpdateProfileRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) return false;

        if (!string.IsNullOrEmpty(request.DisplayName))
        {
            user.DisplayName = request.DisplayName;
            _unitOfWork.Users.Update(user);
        }

        if (user.Role == VCloset.Domain.Enums.UserRole.Customer)
        {
            var profile = await _unitOfWork.CustomerProfiles.FindAsync(c => c.UserInternalId == userId);

            bool isNew = false;
            if (profile == null)
            {
                // Fallback: tạo mới nếu chưa có (lẽ ra đã được tạo khi đăng ký)
                profile = new VCloset.Domain.Entities.CustomerProfile
                {
                    Id = Guid.NewGuid(),
                    UserInternalId = userId,
                    WardrobeItemCount = 0,
                    IsChatBanned = false,
                    IsPostBanned = false,
                    IsOnboardingCompleted = false,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.CustomerProfiles.AddAsync(profile);
                await _unitOfWork.SaveChangesAsync(); // SaveChanges trước để InternalId có giá trị thật
                isNew = true;
            }

            if (request.HeightCm.HasValue) profile.HeightCm = request.HeightCm;
            if (request.WeightKg.HasValue) profile.WeightKg = request.WeightKg;
            if (request.DateOfBirth.HasValue) profile.DateOfBirth = DateTime.SpecifyKind(request.DateOfBirth.Value, DateTimeKind.Utc);
            if (!string.IsNullOrEmpty(request.PhoneNumber)) profile.PhoneNumber = request.PhoneNumber;
            if (!string.IsNullOrEmpty(request.Address)) profile.Address = request.Address;
            if (!string.IsNullOrEmpty(request.Gender)) profile.Gender = request.Gender;
            if (!string.IsNullOrEmpty(request.Country)) profile.Country = request.Country;
            if (!string.IsNullOrEmpty(request.Lifestyle)) profile.Lifestyle = request.Lifestyle;
            if (!string.IsNullOrEmpty(request.EyeColor)) profile.EyeColor = request.EyeColor;
            if (!string.IsNullOrEmpty(request.Hair)) profile.Hair = request.Hair;

            profile.IsOnboardingCompleted = true;
            profile.UpdatedAt = DateTime.UtcNow;

            // Chỉ gọi Update() nếu entity KHÔNG phải vừa mới AddAsync() — tránh lỗi EF Core temporary key
            if (!isNew)
                _unitOfWork.CustomerProfiles.Update(profile);
        }
        else
        {
            var adminProfile = await _unitOfWork.AdminProfiles.FindAsync(a => a.UserInternalId == userId);
            if (adminProfile != null)
            {
                if (!string.IsNullOrEmpty(request.PhoneNumber)) adminProfile.PhoneNumber = request.PhoneNumber;
                _unitOfWork.AdminProfiles.Update(adminProfile);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }


    public async Task<string> UpdateAvatarAsync(int userId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new Exception("File không hợp lệ.");

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) throw new Exception("Không tìm thấy người dùng.");

        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            await _storageService.DeleteFileAsync(user.AvatarUrl);
        }

        using var stream = file.OpenReadStream();
        var fileName = $"avatars/user_{userId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        var newAvatarUrl = await _storageService.UploadFileAsync(stream, fileName, file.ContentType);

        user.AvatarUrl = newAvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return newAvatarUrl;
    }

    public async Task<PublicProfileResponse?> GetPublicProfileAsync(Guid targetUserId)
    {
        var user = await _unitOfWork.Users.FindAsync(u => u.Id == targetUserId);
        if (user == null || !user.IsActive) return null;

        var profile = await _unitOfWork.CustomerProfiles.FindAsync(c => c.UserInternalId == user.InternalId);

        return new PublicProfileResponse
        {
            UserId = user.Id,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            HeightCm = profile?.HeightCm,
            WeightKg = profile?.WeightKg,
            Gender = profile?.Gender,
            WardrobeItemCount = profile?.WardrobeItemCount ?? 0
        };
    }

    public async Task<bool> FollowUserAsync(int currentUserId, Guid targetUserId)
    {
        var targetUser = await _unitOfWork.Users.FindAsync(u => u.Id == targetUserId);
        if (targetUser == null || targetUser.InternalId == currentUserId) return false;

        var existingFollow = await _unitOfWork.UserFollowers.FindAsync(f => f.FollowerId == currentUserId && f.FollowingId == targetUser.InternalId);
            
        if (existingFollow != null) return true;

        var newFollow = new VCloset.Domain.Entities.UserFollower
        {
            FollowerId = currentUserId,
            FollowingId = targetUser.InternalId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.UserFollowers.AddAsync(newFollow);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnfollowUserAsync(int currentUserId, Guid targetUserId)
    {
        var targetUser = await _unitOfWork.Users.FindAsync(u => u.Id == targetUserId);
        if (targetUser == null) return false;

        var existingFollow = await _unitOfWork.UserFollowers.FindAsync(f => f.FollowerId == currentUserId && f.FollowingId == targetUser.InternalId);

        if (existingFollow == null) return true;

        _unitOfWork.UserFollowers.Delete(existingFollow);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<FollowerResponse>> GetMyFollowersAsync(int currentUserId)
    {
        var followers = await _unitOfWork.UserFollowers.FindAllAsync(f => f.FollowingId == currentUserId);
        var result = new System.Collections.Generic.List<FollowerResponse>();
        foreach(var f in followers)
        {
            var user = await _unitOfWork.Users.FindAsync(u => u.InternalId == f.FollowerId);
            if(user != null)
            {
                result.Add(new FollowerResponse { UserId = user.Id, DisplayName = user.DisplayName, AvatarUrl = user.AvatarUrl });
            }
        }
        return result;
    }

    public async Task<IEnumerable<FollowerResponse>> GetMyFollowingAsync(int currentUserId)
    {
        var followings = await _unitOfWork.UserFollowers.FindAllAsync(f => f.FollowerId == currentUserId);
        var result = new System.Collections.Generic.List<FollowerResponse>();
        foreach(var f in followings)
        {
            var user = await _unitOfWork.Users.FindAsync(u => u.InternalId == f.FollowingId);
            if(user != null)
            {
                result.Add(new FollowerResponse { UserId = user.Id, DisplayName = user.DisplayName, AvatarUrl = user.AvatarUrl });
            }
        }
        return result;
    }

    public async Task<bool> DeactivateMyAccountAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || !user.IsActive) return false;

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeactivateUserByAdminAsync(int adminUserId, Guid targetUserId)
    {
        var adminUser = await _unitOfWork.Users.GetByIdAsync(adminUserId);
        var targetUser = await _unitOfWork.Users.FindAsync(u => u.Id == targetUserId);

        if (adminUser == null || targetUser == null) return false;

        if (targetUser.Role == VCloset.Domain.Enums.UserRole.Admin)
        {
            var adminProfile = await _unitOfWork.AdminProfiles.FindAsync(a => a.UserInternalId == adminUserId);
            if (adminProfile == null) throw new Exception("Không tìm thấy AdminProfile.");
            
            var permissionLevel = await _unitOfWork.PermissionLevels.GetByIdAsync(adminProfile.PermissionLevel);
            if (permissionLevel == null || permissionLevel.Name != "super_admin")
            {
                throw new Exception("Bạn không có quyền: Chỉ SuperAdmin mới được phép xóa tài khoản Admin khác.");
            }
        }

        targetUser.IsActive = false;
        targetUser.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(targetUser);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<PublicProfileResponse>> SearchUsersAsync(string searchTerm, int currentUserId)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Trim().Length < 2)
            return Enumerable.Empty<PublicProfileResponse>();

        var keyword = searchTerm.Trim().ToLower();

        var users = await _unitOfWork.Users.FindAllAsync(u =>
            u.IsActive &&
            u.InternalId != currentUserId &&
            u.DisplayName.ToLower().Contains(keyword));

        var result = new System.Collections.Generic.List<PublicProfileResponse>();
        foreach (var user in users.Take(20))
        {
            result.Add(new PublicProfileResponse
            {
                UserId = user.Id,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl,
                // Các trường còn lại để null / mặc định vì đây chỉ là kết quả tìm kiếm nhanh
                HeightCm = null,
                WeightKg = null,
                Gender = null,
                WardrobeItemCount = 0
            });
        }
        return result;
    }
}
