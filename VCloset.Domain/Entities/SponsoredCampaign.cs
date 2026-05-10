using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;

/// <summary>
/// Campaign qu?ng cáo brand partner. display_rank quy?t d?nh th? t? Tab Khám Phá.
/// </summary>
public partial class SponsoredCampaign
{
    public int InternalId { get; set; }

    public Guid Id { get; set; }

    public int BrandInternalId { get; set; }

    public int AffiliateProductInternalId { get; set; }

    public short DisplayRank { get; set; }

    public decimal DailyBudget { get; set; }

    public decimal TotalSpent { get; set; }

    public int ImpressionCount { get; set; }

    public int ClickCount { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AffiliateProduct AffiliateProductInternal { get; set; } = null!;

    public virtual BrandProfile BrandInternal { get; set; } = null!;

    public virtual ICollection<CampaignImpression> CampaignImpressions { get; set; } = new List<CampaignImpression>();
}

