using System;
using System.Collections.Generic;

namespace VCloset.Application.DTOs.Subscriptions.Responses;

/// <summary>
/// DTO chứa thông tin chi tiết của một tài khoản đã đăng ký gói Premium.
/// </summary>
public class PremiumSubscriptionListItem
{
    public Guid SubscriptionId { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string PlanName { get; set; } = null!;
    public string PlanType { get; set; } = null!;
    public decimal PricePaid { get; set; }
    public string Currency { get; set; } = null!;
    public string PaymentMethod { get; set; } = null!;
    public string PaymentRef { get; set; } = null!;
    public DateTime StartedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO phân trang cho danh sách đăng ký Premium.
/// </summary>
public class PagedPremiumSubscriptionsResponse
{
    public IEnumerable<PremiumSubscriptionListItem> Subscriptions { get; set; } = null!;
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
