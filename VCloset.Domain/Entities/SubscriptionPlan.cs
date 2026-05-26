using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;

/// <summary>
/// Cấu hình các gói Premium của hệ thống (phục vụ thanh toán).
/// </summary>
public partial class SubscriptionPlan
{
    public int InternalId { get; set; }

    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string Currency { get; set; } = "VND";

    public int DurationDays { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<PremiumSubscription> PremiumSubscriptions { get; set; } = new List<PremiumSubscription>();
}
