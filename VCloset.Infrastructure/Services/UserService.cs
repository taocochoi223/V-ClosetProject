using System;
using System.Threading.Tasks;
using VCloset.Application.DTOs;
using VCloset.Application.Interfaces;

namespace VCloset.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> UpdateMyProfileAsync(int userId, UpdateProfileRequest request)
    {
        var profile = await _unitOfWork.CustomerProfiles.FindAsync(c => c.UserInternalId == userId);
        
        if (profile == null)
        {
            profile = new VCloset.Domain.Entities.CustomerProfile
            {
                UserInternalId = userId,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.CustomerProfiles.AddAsync(profile);
        }

        if (request.HeightCm.HasValue) profile.HeightCm = request.HeightCm;
        if (request.WeightKg.HasValue) profile.WeightKg = request.WeightKg;
        if (request.DateOfBirth.HasValue) profile.DateOfBirth = request.DateOfBirth;
        if (!string.IsNullOrEmpty(request.PhoneNumber)) profile.PhoneNumber = request.PhoneNumber;
        if (!string.IsNullOrEmpty(request.Address)) profile.Address = request.Address;
        if (!string.IsNullOrEmpty(request.Gender)) profile.Gender = request.Gender;
        if (!string.IsNullOrEmpty(request.Country)) profile.Country = request.Country;

        profile.IsOnboardingCompleted = true;
        profile.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.CustomerProfiles.Update(profile);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}
