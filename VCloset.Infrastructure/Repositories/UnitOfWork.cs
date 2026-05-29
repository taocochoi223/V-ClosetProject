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

    private IGenericRepository<BrandProfile>? _brandProfiles;
    public IGenericRepository<BrandProfile> BrandProfiles => _brandProfiles ??= new GenericRepository<BrandProfile>(_context);

    private IGenericRepository<AffiliateProduct>? _affiliateProducts;
    public IGenericRepository<AffiliateProduct> AffiliateProducts => _affiliateProducts ??= new GenericRepository<AffiliateProduct>(_context);

    private IGenericRepository<AffiliateClick>? _affiliateClicks;
    public IGenericRepository<AffiliateClick> AffiliateClicks => _affiliateClicks ??= new GenericRepository<AffiliateClick>(_context);

    private IGenericRepository<AffiliateConversion>? _affiliateConversions;
    public IGenericRepository<AffiliateConversion> AffiliateConversions => _affiliateConversions ??= new GenericRepository<AffiliateConversion>(_context);

    private IGenericRepository<CanvasOutfit>? _canvasOutfits;
    public IGenericRepository<CanvasOutfit> CanvasOutfits => _canvasOutfits ??= new GenericRepository<CanvasOutfit>(_context);

    private IGenericRepository<Notification>? _notifications;
    public IGenericRepository<Notification> Notifications => _notifications ??= new GenericRepository<Notification>(_context);

    private IGenericRepository<UserDeviceToken>? _userDeviceTokens;
    public IGenericRepository<UserDeviceToken> UserDeviceTokens => _userDeviceTokens ??= new GenericRepository<UserDeviceToken>(_context);

    private IGenericRepository<PaymentTransaction>? _paymentTransactions;
    public IGenericRepository<PaymentTransaction> PaymentTransactions => _paymentTransactions ??= new GenericRepository<PaymentTransaction>(_context);

    private IGenericRepository<SubscriptionPlan>? _subscriptionPlans;
    public IGenericRepository<SubscriptionPlan> SubscriptionPlans => _subscriptionPlans ??= new GenericRepository<SubscriptionPlan>(_context);

    private IGenericRepository<PremiumSubscription>? _premiumSubscriptions;
    public IGenericRepository<PremiumSubscription> PremiumSubscriptions => _premiumSubscriptions ??= new GenericRepository<PremiumSubscription>(_context);

    private IGenericRepository<WardrobeItem>? _wardrobeItems;
    public IGenericRepository<WardrobeItem> WardrobeItems => _wardrobeItems ??= new GenericRepository<WardrobeItem>(_context);

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
