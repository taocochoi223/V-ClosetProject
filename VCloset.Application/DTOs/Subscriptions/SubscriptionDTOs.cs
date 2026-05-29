using System;

namespace VCloset.Application.DTOs.Subscriptions;

/// <summary>
/// Thông tin gói dịch vụ (hiển thị cho client)
/// </summary>
public class SubscriptionPlanResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "VND";
    public int DurationDays { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Trạng thái gói Premium của user hiện tại + số credits còn lại
/// </summary>
public class MySubscriptionResponse
{
    public bool HasActivePremium { get; set; }
    public string? PlanName { get; set; }
    public string? PlanType { get; set; }      // "monthly" | "yearly"
    public DateTime? ExpiresAt { get; set; }
    public int DaysRemaining { get; set; }

    // Credits
    public int BgRemovalCredits { get; set; }
    public int TryOnCredits { get; set; }

    // Wardrobe limits
    public int WardrobeItemCount { get; set; }
    public int? WardrobeItemLimit { get; set; }  // null = không giới hạn
}

/// <summary>
/// Lịch sử giao dịch thanh toán
/// </summary>
public class PaymentTransactionResponse
{
    public Guid Id { get; set; }
    public string PlanName { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string PaymentGateway { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? GatewayTransactionId { get; set; }
    public DateTime CreatedAt { get; set; }
}
