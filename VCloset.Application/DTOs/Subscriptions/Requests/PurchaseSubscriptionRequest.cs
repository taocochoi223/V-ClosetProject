using System;

namespace VCloset.Application.DTOs.Subscriptions.Requests;

/// <summary>
/// Request để khởi tạo thanh toán mua gói Premium
/// </summary>
public class PurchaseSubscriptionRequest
{
    public Guid PlanId { get; set; }
}
