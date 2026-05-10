using VCloset.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;

/// <summary>
/// G�i Premium. Check is_active + expires_at d? enforce gi?i h?n freemium.
/// </summary>
public partial class PremiumSubscription
{
    public int InternalId { get; set; }

    public Guid Id { get; set; }

    public int UserInternalId { get; set; }

    public decimal PricePaid { get; set; }

    public string Currency { get; set; } = null!;

    public DateTime StartedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsActive { get; set; }

    public string? PaymentMethod { get; set; }

    public string? PaymentRef { get; set; }

    public DateTime? CancelledAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User UserInternal { get; set; } = null!;
    [Column("plan_type")]
    public PremiumPlan PlanType { get; set; }
}

