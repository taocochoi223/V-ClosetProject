using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using VCloset.Application.DTOs.Subscriptions.Requests;
using VCloset.Application.Interfaces;

namespace VCloset.API.Controllers;

/// <summary>
/// Quản lý gói dịch vụ Premium: xem gói, trạng thái hiện tại, lịch sử thanh toán, mua gói
/// </summary>
[Route("api/subscriptions")]
[ApiController]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(ISubscriptionService subscriptionService, ILogger<SubscriptionsController> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách tất cả gói dịch vụ Premium đang hoạt động.
    /// </summary>
    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans()
    {
        try
        {
            var plans = await _subscriptionService.GetPlansAsync();
            return Ok(plans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription plans");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy trạng thái gói Premium hiện tại của tôi (credits, ngày hết hạn, giới hạn tủ đồ).
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMySubscription()
    {
        try
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            var result = await _subscriptionService.GetMySubscriptionAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user subscription");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy lịch sử giao dịch thanh toán của tôi.
    /// </summary>
    [HttpGet("transactions")]
    public async Task<IActionResult> GetMyTransactions()
    {
        try
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            var result = await _subscriptionService.GetMyTransactionsAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment transactions");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Khởi tạo giao dịch mua gói Premium. Trả về link thanh toán PayOS.
    /// Body: { "planId": "uuid-of-plan" }
    /// </summary>
    [HttpPost("purchase")]
    public async Task<IActionResult> Purchase([FromBody] PurchaseSubscriptionRequest request)
    {
        try
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

            var paymentResponse = await _subscriptionService.InitiatePurchaseAsync(userId, request.PlanId);
            return Ok(new { 
                payUrl = paymentResponse.PayUrl, 
                deeplink = paymentResponse.Deeplink,
                qrCodeUrl = paymentResponse.QrCodeUrl,
                message = "Vui lòng hoàn tất thanh toán qua link hoặc deeplink." 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating purchase");
            return BadRequest(new { message = ex.Message });
        }
    }
}
