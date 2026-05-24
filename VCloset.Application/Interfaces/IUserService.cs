using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using VCloset.Application.DTOs;
using VCloset.Application.DTOs.Users.Responses;

namespace VCloset.Application.Interfaces;

public interface IUserService
{
    Task<UserProfileResponse?> GetMyProfileAsync(int userId);
    Task<bool> UpdateMyProfileAsync(int userId, UpdateProfileRequest request);
    Task<string> UpdateAvatarAsync(int userId, IFormFile file);
    Task<PublicProfileResponse?> GetPublicProfileAsync(System.Guid targetUserId);
    Task<bool> FollowUserAsync(int currentUserId, System.Guid targetUserId);
    Task<bool> UnfollowUserAsync(int currentUserId, System.Guid targetUserId);
    Task<System.Collections.Generic.IEnumerable<FollowerResponse>> GetMyFollowersAsync(int currentUserId);
    Task<System.Collections.Generic.IEnumerable<FollowerResponse>> GetMyFollowingAsync(int currentUserId);
    Task<bool> DeactivateMyAccountAsync(int userId);
    Task<bool> DeactivateUserByAdminAsync(int adminUserId, System.Guid targetUserId);
}
