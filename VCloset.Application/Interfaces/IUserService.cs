using System.Threading.Tasks;
using VCloset.Application.DTOs;

namespace VCloset.Application.Interfaces;

public interface IUserService
{
    Task<bool> UpdateMyProfileAsync(int userId, UpdateProfileRequest request);
}
