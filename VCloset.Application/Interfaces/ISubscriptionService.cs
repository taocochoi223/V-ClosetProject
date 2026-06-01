using System.Collections.Generic;
using System.Threading.Tasks;
using VCloset.Application.DTOs.Subscriptions.Responses;

namespace VCloset.Application.Interfaces;

public interface ISubscriptionService
{
    /// <summary>GET /api/subscriptions/plans — Danh sách gói dịch vụ</summary>
    Task<IEnumerable<SubscriptionPlanResponse>> GetPlansAsync();

    /// <summary>GET /api/subscriptions/me — Trạng thái gói + credits của user hiện tại</summary>
    Task<MySubscriptionResponse> GetMySubscriptionAsync(int userId);

    /// <summary>GET /api/subscriptions/transactions — Lịch sử thanh toán</summary>
    Task<IEnumerable<PaymentTransactionResponse>> GetMyTransactionsAsync(int userId);

    /// <summary>POST /api/subscriptions/purchase — Tạo pending payment (trả về link PayOS/MoMo/VNPay)</summary>
    Task<VCloset.Application.DTOs.Payment.Responses.PaymentInitializationResponse> InitiatePurchaseAsync(int userId, System.Guid planId, string paymentGateway = "momo");
}
