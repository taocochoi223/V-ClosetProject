using VCloset.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;

/// <summary>
/// S?n ph?m trending sync t? Shopee m?i d�m. T?o tru?c canvas_outfit_items v� c� FK ph? thu?c.
/// </summary>
public partial class AffiliateProduct
{
    public int InternalId { get; set; }

    public Guid Id { get; set; }

    public string ShopeeProductId { get; set; } = null!;

    public string? ShopeeShopId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string ImageUrl { get; set; } = null!;

    public decimal Price { get; set; }

    public decimal? OriginalPrice { get; set; }

    public string AffiliateLink { get; set; } = null!;

    public string TrackingCode { get; set; } = null!;

    public int ClickCount { get; set; }

    public int ConversionCount { get; set; }

    public bool IsTrending { get; set; }

    public bool IsActive { get; set; }

    public DateTime SyncedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AffiliateClick> AffiliateClicks { get; set; } = new List<AffiliateClick>();

    public virtual ICollection<AffiliateConversion> AffiliateConversions { get; set; } = new List<AffiliateConversion>();

    public virtual ICollection<CanvasOutfitItem> CanvasOutfitItems { get; set; } = new List<CanvasOutfitItem>();

    public virtual ICollection<SponsoredCampaign> SponsoredCampaigns { get; set; } = new List<SponsoredCampaign>();
    [Column("category")]
    public ClothingCategory Category { get; set; }
}

