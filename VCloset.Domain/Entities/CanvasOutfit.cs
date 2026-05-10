using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;

/// <summary>
/// Outfit t?o t? Canvas 2D. Ch?a d? t? t? nhà và d? trending affiliate.
/// </summary>
public partial class CanvasOutfit
{
    public int InternalId { get; set; }

    public Guid Id { get; set; }

    public int UserInternalId { get; set; }

    public string? Title { get; set; }

    public string? CanvasSnapshotUrl { get; set; }

    public bool IsPublic { get; set; }

    public int LikeCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AffiliateClick> AffiliateClicks { get; set; } = new List<AffiliateClick>();

    public virtual ICollection<AiLookbook> AiLookbooks { get; set; } = new List<AiLookbook>();

    public virtual ICollection<CanvasOutfitItem> CanvasOutfitItems { get; set; } = new List<CanvasOutfitItem>();

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public virtual ICollection<CommunityPost> CommunityPosts { get; set; } = new List<CommunityPost>();

    public virtual User UserInternal { get; set; } = null!;
}

