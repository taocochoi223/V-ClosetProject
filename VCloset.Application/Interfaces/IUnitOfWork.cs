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
    IGenericRepository<AffiliateProduct> AffiliateProducts { get; }
    IGenericRepository<AffiliateClick> AffiliateClicks { get; }
    IGenericRepository<AffiliateConversion> AffiliateConversions { get; }
    IGenericRepository<CanvasOutfit> CanvasOutfits { get; }
    IGenericRepository<Notification> Notifications { get; }
    IGenericRepository<UserDeviceToken> UserDeviceTokens { get; }
    IGenericRepository<PaymentTransaction> PaymentTransactions { get; }
    IGenericRepository<SubscriptionPlan> SubscriptionPlans { get; }
    IGenericRepository<PremiumSubscription> PremiumSubscriptions { get; }
    IGenericRepository<WardrobeItem> WardrobeItems { get; }
}
