using System;
using System.Collections.Generic;
using System.Net;

namespace VCloset.Domain.Entities;

/// <summary>
/// Log click affiliate. Tính CTR, match conversion, phát hi?n click fraud.
/// </summary>
public partial class AffiliateClick
{
    public Guid Id { get; set; }

    public int? UserInternalId { get; set; }

    public int AffiliateProductInternalId { get; set; }

    public int? OutfitInternalId { get; set; }

    public string ClickSource { get; set; } = null!;

    public IPAddress? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime ClickedAt { get; set; }

    public virtual ICollection<AffiliateConversion> AffiliateConversions { get; set; } = new List<AffiliateConversion>();

    public virtual AffiliateProduct AffiliateProductInternal { get; set; } = null!;

    public virtual CanvasOutfit? OutfitInternal { get; set; }

    public virtual User? UserInternal { get; set; }
}

