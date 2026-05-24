using VCloset.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;


public partial class User
{
    public int InternalId { get; set; }

    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string? GoogleId { get; set; }

    public string DisplayName { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public bool IsActive { get; set; }

    public bool IsEmailVerified { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AdminPermission> AdminPermissionGrantedByInternalNavigations { get; set; } = new List<AdminPermission>();

    public virtual ICollection<AdminPermission> AdminPermissionUserInternals { get; set; } = new List<AdminPermission>();

    public virtual AdminProfile? AdminProfile { get; set; }

    public virtual ICollection<AffiliateClick> AffiliateClicks { get; set; } = new List<AffiliateClick>();

    public virtual ICollection<AffiliateConversion> AffiliateConversions { get; set; } = new List<AffiliateConversion>();

    public virtual ICollection<AiLookbook> AiLookbooks { get; set; } = new List<AiLookbook>();

    public virtual BrandProfile? BrandProfile { get; set; }

    public virtual ICollection<CampaignImpression> CampaignImpressions { get; set; } = new List<CampaignImpression>();

    public virtual ICollection<CanvasOutfit> CanvasOutfits { get; set; } = new List<CanvasOutfit>();

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public virtual ICollection<ChatRoomMember> ChatRoomMembers { get; set; } = new List<ChatRoomMember>();

    public virtual ICollection<ChatRoom> ChatRooms { get; set; } = new List<ChatRoom>();

    public virtual ICollection<CommunityPost> CommunityPosts { get; set; } = new List<CommunityPost>();

    public virtual CustomerProfile? CustomerProfile { get; set; }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<PostComment> PostComments { get; set; } = new List<PostComment>();

    public virtual ICollection<PostLike> PostLikes { get; set; } = new List<PostLike>();

    public virtual ICollection<PostReport> PostReportReporterInternals { get; set; } = new List<PostReport>();

    public virtual ICollection<PostReport> PostReportResolvedByInternalNavigations { get; set; } = new List<PostReport>();

    public virtual ICollection<PremiumSubscription> PremiumSubscriptions { get; set; } = new List<PremiumSubscription>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual ICollection<UserBanLog> UserBanLogBannedByInternalNavigations { get; set; } = new List<UserBanLog>();

    public virtual ICollection<UserBanLog> UserBanLogLiftedByInternalNavigations { get; set; } = new List<UserBanLog>();

    public virtual ICollection<UserBanLog> UserBanLogUserInternals { get; set; } = new List<UserBanLog>();

    public virtual ICollection<WardrobeItem> WardrobeItems { get; set; } = new List<WardrobeItem>();
    [Column("role")]
    public UserRole Role { get; set; }

    [Column("auth_provider")]
    public AuthProvider AuthProvider { get; set; }
}

