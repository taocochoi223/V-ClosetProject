using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;

/// <summary>
/// Log impression sponsored. Volume cao — cân nh?c partition theo tháng khi scale.
/// </summary>
public partial class CampaignImpression
{
    public long Id { get; set; }

    public int CampaignInternalId { get; set; }

    public int? UserInternalId { get; set; }

    public DateTime ImpressedAt { get; set; }

    public virtual SponsoredCampaign CampaignInternal { get; set; } = null!;

    public virtual User? UserInternal { get; set; }
}

