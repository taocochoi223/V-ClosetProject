using System;
using System.Threading.Tasks;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Infrastructure.Data;

namespace VCloset.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly VClosetVersion30Context _context;
    private IGenericRepository<User>? _users;

    public UnitOfWork(VClosetVersion30Context context)
    {
        _context = context;
    }

    public IGenericRepository<User> Users => _users ??= new GenericRepository<User>(_context);

    private IGenericRepository<RefreshToken>? _refreshTokens;
    public IGenericRepository<RefreshToken> RefreshTokens => _refreshTokens ??= new GenericRepository<RefreshToken>(_context);

    private IGenericRepository<CustomerProfile>? _customerProfiles;
    public IGenericRepository<CustomerProfile> CustomerProfiles => _customerProfiles ??= new GenericRepository<CustomerProfile>(_context);

    private IGenericRepository<UserFollower>? _userFollowers;
    public IGenericRepository<UserFollower> UserFollowers => _userFollowers ??= new GenericRepository<UserFollower>(_context);

    private IGenericRepository<AdminProfile>? _adminProfiles;
    public IGenericRepository<AdminProfile> AdminProfiles => _adminProfiles ??= new GenericRepository<AdminProfile>(_context);

    private IGenericRepository<PermissionLevel>? _permissionLevels;
    public IGenericRepository<PermissionLevel> PermissionLevels => _permissionLevels ??= new GenericRepository<PermissionLevel>(_context);

    private IGenericRepository<UserBanLog>? _userBanLogs;
    public IGenericRepository<UserBanLog> UserBanLogs => _userBanLogs ??= new GenericRepository<UserBanLog>(_context);

    private IGenericRepository<AdminPermission>? _adminPermissions;
    public IGenericRepository<AdminPermission> AdminPermissions => _adminPermissions ??= new GenericRepository<AdminPermission>(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
