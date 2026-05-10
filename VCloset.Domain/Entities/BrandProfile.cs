using VCloset.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;

/// <summary>
/// Profile brand partner B2B. Admin verify tru?c khi ch?y sponsored campaign.
/// </summary>
public partial class BrandProfile
{
    public int InternalId { get; set; }

    public Guid Id { get; set; }

    public int UserInternalId { get; set; }

    public string BrandName { get; set; } = null!;

    public string? LogoUrl { get; set; }

    public string? WebsiteUrl { get; set; }

    public string? ContactPhone { get; set; }

    public string? TaxCode { get; set; }

    public decimal CreditBalance { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<SponsoredCampaign> SponsoredCampaigns { get; set; } = new List<SponsoredCampaign>();

    public virtual User UserInternal { get; set; } = null!;
    [Column("status")]
    public BrandStatus Status { get; set; }
}

