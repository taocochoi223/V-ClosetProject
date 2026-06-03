using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Domain.Enums;

namespace VCloset.Infrastructure.Services;

/// <summary>
/// Triển khai luồng thanh toán chuyển khoản thủ công.
/// 
/// Chiến lược lưu trữ (KHÔNG cần migration DB):
/// - PaymentGateway = "manual_transfer"
/// - Status = Pending (dùng lại trạng thái hiện có)
/// - RawCallbackData = JSON string chứa: proofImageUrl, userNote, adminNote, reviewedAt, reviewedByAdminId
/// - GatewayTransactionId = proofImageUrl (để truy cập nhanh URL ảnh bill)
/// </summary>
public class ManualPaymentService : IManualPaymentService
{
    private const string GatewayName = "manual_transfer";

    private readonly IUnitOfWork _unitOfWork;

    public ManualPaymentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task<ManualPaymentResult> SubmitManualPaymentAsync(
        int userId,
        Guid planId,
        string proofImageUrl,
        string? userNote)
    {
        // Tìm gói dịch vụ hợp lệ
        var plan = await _unitOfWork.SubscriptionPlans.FindAsync(p => p.Id == planId && p.IsActive);
        if (plan == null)
            throw new Exception("Gói dịch vụ không tồn tại hoặc đã ngừng cung cấp.");

        // Kiểm tra xem user có đang có giao dịch manual_transfer nào đang Pending không
        var existingPending = await _unitOfWork.PaymentTransactions.FindAsync(t =>
            t.UserInternalId == userId &&
            t.PaymentGateway == GatewayName &&
            t.Status == PaymentStatus.Pending);

        if (existingPending != null)
            throw new Exception("Bạn đã có một giao dịch chuyển khoản đang chờ duyệt. Vui lòng đợi admin xem xét trước khi nộp thêm.");

        // Tạo dữ liệu proof dạng JSON lưu vào RawCallbackData
        var proofData = new
        {
            proofImageUrl,
            userNote,
            adminNote = (string?)null,
            reviewedAt = (DateTime?)null,
            reviewedByAdminId = (int?)null,
            submittedAt = DateTime.UtcNow
        };

        var transaction = new PaymentTransaction
        {
            Id                         = Guid.NewGuid(),
            UserInternalId             = userId,
            SubscriptionPlanInternalId = plan.InternalId,
            Amount                     = plan.Price,
            Currency                   = plan.Currency,
            PaymentGateway             = GatewayName,
            Status                     = PaymentStatus.Pending,
            GatewayTransactionId       = proofImageUrl, // lưu URL ảnh bill để truy cập nhanh
            RawCallbackData            = JsonSerializer.Serialize(proofData),
            CreatedAt                  = DateTime.UtcNow,
            UpdatedAt                  = DateTime.UtcNow
        };

        await _unitOfWork.PaymentTransactions.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        return new ManualPaymentResult
        {
            TransactionId = transaction.InternalId,
            Status        = "pending",
            Message       = "Đã nộp chứng từ thành công. Giao dịch của bạn đang chờ admin xác nhận."
        };
    }

    /// <inheritdoc/>
    public async Task<List<ManualPaymentListItem>> GetPendingManualPaymentsAsync()
    {
        // Lấy tất cả giao dịch manual_transfer đang Pending
        var transactions = await _unitOfWork.PaymentTransactions.FindAllAsync(t =>
            t.PaymentGateway == GatewayName &&
            t.Status == PaymentStatus.Pending);

        var result = new List<ManualPaymentListItem>();

        foreach (var t in transactions.OrderByDescending(t => t.CreatedAt))
        {
            var user = await _unitOfWork.Users.GetByIdAsync(t.UserInternalId);
            var plan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(t.SubscriptionPlanInternalId);

            // Parse RawCallbackData để lấy proofImageUrl và userNote
            string? proofImageUrl = t.GatewayTransactionId; // fallback
            string? userNote = null;

            if (!string.IsNullOrEmpty(t.RawCallbackData))
            {
                try
                {
                    using var doc = JsonDocument.Parse(t.RawCallbackData);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("proofImageUrl", out var urlProp))
                        proofImageUrl = urlProp.GetString();
                    if (root.TryGetProperty("userNote", out var noteProp))
                        userNote = noteProp.GetString();
                }
                catch { /* ignore parse errors */ }
            }

            result.Add(new ManualPaymentListItem
            {
                TransactionId  = t.InternalId,
                TransactionGuid = t.Id,
                UserId         = t.UserInternalId,
                UserEmail      = user?.Email ?? string.Empty,
                UserName       = user?.DisplayName ?? string.Empty,
                PlanName       = plan?.Name ?? "Không xác định",
                Amount         = t.Amount,
                Currency       = t.Currency,
                ProofImageUrl  = proofImageUrl,
                UserNote       = userNote,
                CreatedAt      = t.CreatedAt
            });
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task ApproveManualPaymentAsync(int adminId, int transactionId, string? adminNote)
    {
        var transaction = await _unitOfWork.PaymentTransactions.GetByIdAsync(transactionId)
            ?? throw new Exception($"Không tìm thấy giao dịch với ID = {transactionId}.");

        if (transaction.PaymentGateway != GatewayName)
            throw new Exception("Giao dịch này không phải là chuyển khoản thủ công.");

        if (transaction.Status != PaymentStatus.Pending)
            throw new Exception("Giao dịch này không ở trạng thái chờ duyệt.");

        // Cập nhật trạng thái giao dịch
        transaction.Status    = PaymentStatus.Success;
        transaction.UpdatedAt = DateTime.UtcNow;

        // Cập nhật RawCallbackData với thông tin admin review
        transaction.RawCallbackData = UpdateProofData(transaction.RawCallbackData, adminNote, adminId, DateTime.UtcNow);

        // Tìm plan để tính số ngày Premium
        var plan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(transaction.SubscriptionPlanInternalId);
        if (plan != null)
        {
            var existingPremium = await _unitOfWork.PremiumSubscriptions.FindAsync(
                ps => ps.UserInternalId == transaction.UserInternalId && ps.IsActive);

            if (existingPremium != null)
            {
                // Kéo dài subscription hiện tại
                existingPremium.ExpiresAt = existingPremium.ExpiresAt > DateTime.UtcNow
                    ? existingPremium.ExpiresAt.AddDays(plan.DurationDays)
                    : DateTime.UtcNow.AddDays(plan.DurationDays);
            }
            else
            {
                // Tạo mới PremiumSubscription
                var newPremium = new PremiumSubscription
                {
                    Id                        = Guid.NewGuid(),
                    UserInternalId            = transaction.UserInternalId,
                    SubscriptionPlanInternalId = plan.InternalId,
                    PlanType                  = plan.DurationDays >= 365 ? PremiumPlan.Yearly : PremiumPlan.Monthly,
                    PricePaid                 = transaction.Amount,
                    Currency                  = transaction.Currency,
                    PaymentMethod             = GatewayName,
                    PaymentRef                = transaction.Id.ToString(),
                    StartedAt                 = DateTime.UtcNow,
                    ExpiresAt                 = DateTime.UtcNow.AddDays(plan.DurationDays),
                    IsActive                  = true,
                    CreatedAt                 = DateTime.UtcNow
                };
                await _unitOfWork.PremiumSubscriptions.AddAsync(newPremium);
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task RejectManualPaymentAsync(int adminId, int transactionId, string? adminNote)
    {
        var transaction = await _unitOfWork.PaymentTransactions.GetByIdAsync(transactionId)
            ?? throw new Exception($"Không tìm thấy giao dịch với ID = {transactionId}.");

        if (transaction.PaymentGateway != GatewayName)
            throw new Exception("Giao dịch này không phải là chuyển khoản thủ công.");

        if (transaction.Status != PaymentStatus.Pending)
            throw new Exception("Giao dịch này không ở trạng thái chờ duyệt.");

        transaction.Status    = PaymentStatus.Failed;
        transaction.UpdatedAt = DateTime.UtcNow;

        // Cập nhật RawCallbackData với thông tin admin review
        transaction.RawCallbackData = UpdateProofData(transaction.RawCallbackData, adminNote, adminId, DateTime.UtcNow);

        await _unitOfWork.SaveChangesAsync();
    }

    // ─── Private Helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Ghi đè các trường admin-review vào JSON hiện có của RawCallbackData.
    /// </summary>
    private static string UpdateProofData(string? existingJson, string? adminNote, int adminId, DateTime reviewedAt)
    {
        // Parse existing data
        var dict = new Dictionary<string, object?>();

        if (!string.IsNullOrEmpty(existingJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(existingJson);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String  => (object?)prop.Value.GetString(),
                        JsonValueKind.Number  => prop.Value.TryGetInt32(out var i) ? i : prop.Value.GetDouble(),
                        JsonValueKind.True    => true,
                        JsonValueKind.False   => false,
                        JsonValueKind.Null    => null,
                        _                    => prop.Value.GetRawText()
                    };
                }
            }
            catch { /* ignore */ }
        }

        dict["adminNote"]          = adminNote;
        dict["reviewedByAdminId"]  = adminId;
        dict["reviewedAt"]         = reviewedAt.ToString("o");

        return JsonSerializer.Serialize(dict);
    }
}
