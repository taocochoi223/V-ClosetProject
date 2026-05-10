using VCloset.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;

/// <summary>
/// �on h�ng th�nh c�ng qua affiliate. commission_rate snapshot t?i th?i di?m chuy?n d?i.
/// </summary>
public partial class AffiliateConversion
{
    public Guid Id { get; set; }

    public Guid? ClickId { get; set; }

    public int? UserInternalId { get; set; }

    public int AffiliateProductInternalId { get; set; }

    public string? ShopeeOrderId { get; set; }

    public decimal OrderAmount { get; set; }

    public decimal CommissionRate { get; set; }

    public decimal CommissionAmount { get; set; }

    public DateTime ConvertedAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public virtual AffiliateProduct AffiliateProductInternal { get; set; } = null!;

    public virtual AffiliateClick? Click { get; set; }

    public virtual User? UserInternal { get; set; }
    [Column("status")]
    public CommissionStatus Status { get; set; }
}

