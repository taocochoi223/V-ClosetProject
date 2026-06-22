using System;

namespace VCloset.Application.DTOs.Subscriptions.Requests;

/// <summary>
/// Request để khởi tạo thanh toán mua gói Premium
/// </summary>
public class PurchaseSubscriptionRequest
{
    public Guid PlanId { get; set; }
    
    /// <summary>
    /// Cổng thanh toán (payos). Mặc định là payos.
    /// </summary>
    public string PaymentGateway { get; set; } = "payos";
}
