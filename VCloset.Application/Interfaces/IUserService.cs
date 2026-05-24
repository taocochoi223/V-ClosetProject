using System.Threading.Tasks;
using VCloset.Application.DTOs;

namespace VCloset.Application.Interfaces;

public interface IUserService
{
    Task<UserProfileResponse?> GetMyProfileAsync(int userId);
    Task<bool> UpdateMyProfileAsync(int userId, UpdateProfileRequest request);
}
