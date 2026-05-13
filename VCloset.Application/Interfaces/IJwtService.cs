using VCloset.Domain.Entities;

namespace VCloset.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
