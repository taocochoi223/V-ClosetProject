using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VCloset.Application.Interfaces;

/// <summary>
/// DTO trả về khi user submit proof thanh toán thủ công.
/// </summary>
public class ManualPaymentResult
{
    public int TransactionId { get; set; }
    public string Status { get; set; } = null!;
    public string Message { get; set; } = null!;
}

/// <summary>
/// DTO dùng cho admin xem danh sách các giao dịch chuyển khoản chờ duyệt.
/// </summary>
public class ManualPaymentListItem
{
    public int TransactionId { get; set; }
    public Guid TransactionGuid { get; set; }
    public int UserId { get; set; }
    public string UserEmail { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string PlanName { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public string? ProofImageUrl { get; set; }
    public string? UserNote { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Service xử lý luồng thanh toán chuyển khoản thủ công.
/// Không yêu cầu migration DB — lưu toàn bộ thông tin proof vào RawCallbackData (JSON).
/// </summary>
public interface IManualPaymentService
{
    /// <summary>
    /// User nộp proof chuyển khoản: tạo PaymentTransaction (Status=Pending, Gateway=manual_transfer)
    /// và lưu thông tin ảnh bill vào RawCallbackData dưới dạng JSON.
    /// </summary>
    Task<ManualPaymentResult> SubmitManualPaymentAsync(int userId, Guid planId, string proofImageUrl, string? userNote);

    /// <summary>
    /// Admin: lấy danh sách tất cả giao dịch chuyển khoản thủ công đang chờ duyệt.
    /// </summary>
    Task<List<ManualPaymentListItem>> GetPendingManualPaymentsAsync();

    /// <summary>
    /// Admin: duyệt giao dịch, kích hoạt gói Premium cho user.
    /// </summary>
    Task ApproveManualPaymentAsync(int adminId, int transactionId, string? adminNote);

    /// <summary>
    /// Admin: từ chối giao dịch (Status → Failed).
    /// </summary>
    Task RejectManualPaymentAsync(int adminId, int transactionId, string? adminNote);
}
