using System;

namespace VCloset.Application.DTOs.Subscriptions.Responses;

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
