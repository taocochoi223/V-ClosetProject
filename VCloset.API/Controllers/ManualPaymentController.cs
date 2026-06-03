using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VCloset.Application.Interfaces;
using VCloset.Infrastructure.Security;

namespace VCloset.API.Controllers;

/// <summary>
/// Quản lý luồng thanh toán chuyển khoản thủ công (Manual Bank Transfer).
/// - User: nộp chứng từ (bill ảnh), upload ảnh bill
/// - Admin: xem danh sách chờ duyệt, duyệt, từ chối
/// </summary>
[Route("api/manual-payments")]
[ApiController]
public class ManualPaymentController : ControllerBase
{
    private readonly IManualPaymentService _manualPaymentService;
    private readonly IStorageService _storageService;
    private readonly ILogger<ManualPaymentController> _logger;

    public ManualPaymentController(
        IManualPaymentService manualPaymentService,
        IStorageService storageService,
        ILogger<ManualPaymentController> logger)
    {
        _manualPaymentService = manualPaymentService;
        _storageService       = storageService;
        _logger               = logger;
    }

    // ─── User Endpoints ───────────────────────────────────────────────────────

    /// <summary>
    /// Upload ảnh bill/chứng từ chuyển khoản lên storage.
    /// Trả về URL của ảnh để dùng trong /submit.
    /// </summary>
    [HttpPost("upload-proof")]
    [Authorize]
    public async Task<IActionResult> UploadProof(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "File không được để trống." });

            // Giới hạn loại file ảnh hợp lệ
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
            if (!Array.Exists(allowedTypes, t => t == file.ContentType.ToLower()))
                return BadRequest(new { message = "Chỉ chấp nhận file ảnh (jpeg, jpg, png, webp)." });

            // Giới hạn kích thước: 10 MB
            if (file.Length > 10 * 1024 * 1024)
                return BadRequest(new { message = "File ảnh không được vượt quá 10MB." });

            var fileName = $"{Guid.NewGuid()}{System.IO.Path.GetExtension(file.FileName)}";
            await using var stream = file.OpenReadStream();
            var url = await _storageService.UploadFileAsync(stream, fileName, file.ContentType, "payment-proofs");

            return Ok(new { url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi upload ảnh bill chuyển khoản.");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// User nộp chứng từ thanh toán chuyển khoản thủ công.
    /// Tạo giao dịch trạng thái Pending, lưu URL ảnh bill.
    /// </summary>
    [HttpPost("submit")]
    [Authorize]
    public async Task<IActionResult> SubmitManualPayment([FromBody] SubmitManualPaymentRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId))
                return Unauthorized(new { message = "Không xác định được tài khoản." });

            if (!Guid.TryParse(request.PlanId, out var planGuid))
                return BadRequest(new { message = "planId không hợp lệ." });

            var result = await _manualPaymentService.SubmitManualPaymentAsync(
                userId,
                planGuid,
                request.ProofImageUrl,
                request.UserNote);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi user nộp chứng từ chuyển khoản.");
            return BadRequest(new { message = ex.Message });
        }
    }

    // ─── Admin Endpoints ──────────────────────────────────────────────────────

    /// <summary>
    /// Admin: lấy danh sách tất cả giao dịch chuyển khoản thủ công đang chờ duyệt.
    /// </summary>
    [HttpGet("admin/pending")]
    [RequirePermission("payment.manage")]
    public async Task<IActionResult> GetPendingManualPayments()
    {
        try
        {
            var list = await _manualPaymentService.GetPendingManualPaymentsAsync();
            return Ok(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách giao dịch chuyển khoản chờ duyệt.");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Admin: duyệt một giao dịch chuyển khoản thủ công → kích hoạt Premium.
    /// </summary>
    [HttpPost("admin/{transactionId:int}/approve")]
    [RequirePermission("payment.manage")]
    public async Task<IActionResult> ApproveManualPayment(int transactionId, [FromBody] AdminReviewRequest? request)
    {
        try
        {
            var adminIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdString, out int adminId))
                return Unauthorized(new { message = "Không xác định được tài khoản admin." });

            await _manualPaymentService.ApproveManualPaymentAsync(adminId, transactionId, request?.AdminNote);
            return Ok(new { message = "Đã duyệt giao dịch thành công. Premium đã được kích hoạt cho user." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi duyệt giao dịch ID={transactionId}.");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Admin: từ chối một giao dịch chuyển khoản thủ công.
    /// </summary>
    [HttpPost("admin/{transactionId:int}/reject")]
    [RequirePermission("payment.manage")]
    public async Task<IActionResult> RejectManualPayment(int transactionId, [FromBody] AdminReviewRequest? request)
    {
        try
        {
            var adminIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdString, out int adminId))
                return Unauthorized(new { message = "Không xác định được tài khoản admin." });

            await _manualPaymentService.RejectManualPaymentAsync(adminId, transactionId, request?.AdminNote);
            return Ok(new { message = "Đã từ chối giao dịch." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi từ chối giao dịch ID={transactionId}.");
            return BadRequest(new { message = ex.Message });
        }
    }
}

// ─── Request DTOs (inline) ────────────────────────────────────────────────────

/// <summary>User nộp chứng từ chuyển khoản.</summary>
public class SubmitManualPaymentRequest
{
    /// <summary>Guid của gói dịch vụ muốn mua.</summary>
    public string PlanId { get; set; } = null!;

    /// <summary>URL ảnh bill sau khi upload qua /upload-proof.</summary>
    public string ProofImageUrl { get; set; } = null!;

    /// <summary>Ghi chú tùy chọn từ user.</summary>
    public string? UserNote { get; set; }
}

/// <summary>Admin duyệt hoặc từ chối giao dịch.</summary>
public class AdminReviewRequest
{
    /// <summary>Ghi chú từ admin (lý do duyệt/từ chối).</summary>
    public string? AdminNote { get; set; }
}
