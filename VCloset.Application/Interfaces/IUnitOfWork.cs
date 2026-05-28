using System;
using System.Threading.Tasks;
using VCloset.Domain.Entities;

namespace VCloset.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<User> Users { get; }
    Task<int> SaveChangesAsync();
    IGenericRepository<RefreshToken> RefreshTokens { get; }
    IGenericRepository<CustomerProfile> CustomerProfiles { get; }
    IGenericRepository<UserFollower> UserFollowers { get; }
    IGenericRepository<AdminProfile> AdminProfiles { get; }
    IGenericRepository<PermissionLevel> PermissionLevels { get; }
    IGenericRepository<UserBanLog> UserBanLogs { get; }
    IGenericRepository<AdminPermission> AdminPermissions { get; }
    IGenericRepository<BrandProfile> BrandProfiles { get; }
    IGenericRepository<Notification> Notifications { get; }
    IGenericRepository<UserDeviceToken> UserDeviceTokens { get; }
}
