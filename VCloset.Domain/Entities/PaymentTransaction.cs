using System;
using VCloset.Domain.Enums;

namespace VCloset.Domain.Entities;

/// <summary>
/// Bảng ghi nhận giao dịch thanh toán qua ví điện tử
/// </summary>
public partial class PaymentTransaction
{
    public int InternalId { get; set; }

    public Guid Id { get; set; }

    public int UserInternalId { get; set; }

    public int SubscriptionPlanInternalId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "VND";

    public string PaymentGateway { get; set; } = null!;

    public PaymentStatus Status { get; set; }

    public string? GatewayTransactionId { get; set; }

    public string? RawCallbackData { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User UserInternal { get; set; } = null!;

    public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;
}
