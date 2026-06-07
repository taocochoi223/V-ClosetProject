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
    private readonly INotificationHubService _notificationHubService;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly ITierConfigService _tierConfigService;

    public ManualPaymentService(
        IUnitOfWork unitOfWork,
        INotificationHubService notificationHubService,
        INotificationService notificationService,
        IEmailService emailService,
        ITierConfigService tierConfigService)
    {
        _unitOfWork = unitOfWork;
        _notificationHubService = notificationHubService;
        _notificationService = notificationService;
        _emailService = emailService;
        _tierConfigService = tierConfigService;
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

        // Gửi SignalR alert đến Admin
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            var alertItem = new ManualPaymentListItem
            {
                TransactionId = transaction.InternalId,
                TransactionGuid = transaction.Id,
                UserId = userId,
                UserEmail = user?.Email ?? string.Empty,
                UserName = user?.DisplayName ?? string.Empty,
                PlanName = plan.Name,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                ProofImageUrl = proofImageUrl,
                UserNote = userNote,
                CreatedAt = transaction.CreatedAt
            };
            await _notificationHubService.SendPendingPaymentAlertAsync(alertItem);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignalR Admin Alert Error: {ex.Message}");
        }

        // Gửi Email thông báo đến các Admin
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            var admins = await _unitOfWork.Users.FindAllAsync(u => u.Role == UserRole.Admin && u.IsActive);
            
            if (user != null && admins != null && admins.Any())
            {

                foreach (var admin in admins)
                {
                    if (!string.IsNullOrEmpty(admin.Email))
                    {
                        await _emailService.SendAdminPaymentNotificationAsync(
                            admin.Email, 
                            user?.DisplayName ?? "Người dùng ẩn danh",
                            user?.Email ?? "N/A",
                            plan.Name,
                            transaction.Amount, 
                            transaction.Currency,
                            userNote ?? string.Empty, 
                            transaction.CreatedAt
                        );
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Email Notification Alert Error: {ex.Message}");
        }

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

        // Tìm plan để tính số ngày Premium hoặc số lượt nạp lẻ
        var plan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(transaction.SubscriptionPlanInternalId);
        bool isTopup = false;
        int addedCredits = 10;

        if (plan != null)
        {
            var profile = await _unitOfWork.CustomerProfiles.FindAsync(cp => cp.UserInternalId == transaction.UserInternalId);
            isTopup = plan.Name.ToLower().Contains("credit") || plan.Name.ToLower().Contains("lượt lẻ") || plan.Name.ToLower().Contains("lượt thử");

            if (isTopup)
            {
                // TRƯỜNG HỢP 1 & 2: NẠP LƯỢL LẺ (Cộng dồn lượt dùng hiện tại)
                if (profile != null)
                {
                    // Tự động phân tích số lượng credits từ tên gói (Ví dụ: "Gói 10 Credits" -> 10, "Gói 25 Credits" -> 25)
                    var match = System.Text.RegularExpressions.Regex.Match(plan.Name, @"\d+");
                    if (match.Success && int.TryParse(match.Value, out int parsedVal))
                    {
                        addedCredits = parsedVal;
                    }

                    profile.TryOnCredits += addedCredits;
                    // Mặc định gói credits lẻ thử đồ AI thường đi kèm một số lượt xóa nền tương đương (nếu cần thiết, hoặc chỉ cộng try_on)
                    profile.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.CustomerProfiles.Update(profile);
                }
            }
            else
            {
                // TRƯỜNG HỢP 3: NÂNG CẤP GÓI PREMIUM (Gia hạn thời gian sử dụng)
                var existingPremium = await _unitOfWork.PremiumSubscriptions.FindAsync(
                    ps => ps.UserInternalId == transaction.UserInternalId && ps.IsActive);

                if (existingPremium != null)
                {
                    // Kéo dài subscription hiện tại
                    if (plan.DurationDays.HasValue)
                    {
                        existingPremium.ExpiresAt = (existingPremium.ExpiresAt.HasValue && existingPremium.ExpiresAt.Value > DateTime.UtcNow)
                            ? existingPremium.ExpiresAt.Value.AddDays(plan.DurationDays.Value)
                            : DateTime.UtcNow.AddDays(plan.DurationDays.Value);
                    }
                    else
                    {
                        existingPremium.ExpiresAt = null;
                    }
                }
                else
                {
                    // Tạo mới PremiumSubscription
                    var newPremium = new PremiumSubscription
                    {
                        Id                        = Guid.NewGuid(),
                        UserInternalId            = transaction.UserInternalId,
                        SubscriptionPlanInternalId = plan.InternalId,
                        PlanType                  = !plan.DurationDays.HasValue || plan.DurationDays >= 365 ? PremiumPlan.Yearly : PremiumPlan.Monthly,
                        PricePaid                 = transaction.Amount,
                        Currency                  = transaction.Currency,
                        PaymentMethod             = GatewayName,
                        PaymentRef                = transaction.Id.ToString(),
                        StartedAt                 = DateTime.UtcNow,
                        ExpiresAt = plan.DurationDays.HasValue ? DateTime.UtcNow.AddDays(plan.DurationDays.Value) : (DateTime?)null,
                        IsActive                  = true,
                        CreatedAt                 = DateTime.UtcNow
                    };
                    await _unitOfWork.PremiumSubscriptions.AddAsync(newPremium);
                }

                // Cập nhật lượt AI cho khách hàng theo cấu hình của premium tier
                if (profile != null)
                {
                    var premiumTier = await _tierConfigService.GetConfigEntityAsync("premium");
                    profile.BgRemovalCredits = premiumTier.BgRemovalCredits;
                    profile.TryOnCredits = premiumTier.TryOnCredits;
                    profile.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.CustomerProfiles.Update(profile);
                }
            }
        }

        await _unitOfWork.SaveChangesAsync();

        // Cấu hình thông điệp thông báo tuỳ biến
        string successMessage = isTopup 
            ? $"Giao dịch chuyển khoản của bạn đã được phê duyệt thành công! Cộng thêm {addedCredits} lượt thử đồ AI."
            : "Giao dịch chuyển khoản của bạn đã được phê duyệt thành công! Premium đã được kích hoạt.";

        // Lưu thông báo vào CSDL và gửi Real-time Notification qua SignalR
        try
        {
            await _notificationService.SendNotificationAsync(
                transaction.UserInternalId,
                "Payment",
                "Thanh toán thành công",
                successMessage,
                "ManualPayment",
                transaction.InternalId
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database Notification Error: {ex.Message}");
        }

        // Gửi SignalR update đến user
        try
        {
            await _notificationHubService.SendPaymentUpdateAsync(transaction.UserInternalId, new {
                transactionId = transaction.InternalId,
                status = "success",
                message = successMessage
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignalR User Alert Error: {ex.Message}");
        }
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

        // Lưu thông báo vào CSDL và gửi Real-time Notification qua SignalR
        try
        {
            await _notificationService.SendNotificationAsync(
                transaction.UserInternalId,
                "Payment",
                "Thanh toán thất bại",
                $"Giao dịch chuyển khoản của bạn đã bị từ chối. Lý do: {adminNote ?? "Không có lý do cụ thể"}",
                "ManualPayment",
                transaction.InternalId
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database Notification Error: {ex.Message}");
        }

        // Gửi SignalR update đến user
        try
        {
            await _notificationHubService.SendPaymentUpdateAsync(transaction.UserInternalId, new {
                transactionId = transaction.InternalId,
                status = "failed",
                message = $"Giao dịch chuyển khoản của bạn đã bị từ chối. Lý do: {adminNote ?? "Không có lý do cụ thể"}"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignalR User Alert Error: {ex.Message}");
        }
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
