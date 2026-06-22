using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VCloset.Application.DTOs.Subscriptions.Responses;
using VCloset.Application.Interfaces;
using VCloset.Infrastructure.Security;

namespace VCloset.API.Controllers;

/// <summary>
/// Quản lý danh sách và thu hồi gói Premium của người dùng dành cho Admin.
/// </summary>
[Route("api/admin/subscriptions")]
[ApiController]
[Authorize]
public class AdminPremiumSubscriptionsController : ControllerBase
{
    private readonly IAdminSubscriptionService _adminSubscriptionService;

    public AdminPremiumSubscriptionsController(IAdminSubscriptionService adminSubscriptionService)
    {
        _adminSubscriptionService = adminSubscriptionService;
    }

    /// <summary>
    /// API lấy danh sách tài khoản đã đăng ký gói Premium (phân trang, lọc, tìm kiếm).
    /// </summary>
    [RequirePermission("subscription.manage")]
    [HttpGet]
    public async Task<IActionResult> GetPremiumSubscriptions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? planType = null)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var result = await _adminSubscriptionService.GetPremiumSubscriptionsAsync(page, pageSize, search, isActive, planType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// API thu hồi/hủy gói Premium của một tài khoản cụ thể.
    /// </summary>
    [RequirePermission("subscription.manage")]
    [HttpPost("{id:guid}/revoke")]
    public async Task<IActionResult> RevokeSubscription(Guid id, [FromBody] RevokeSubscriptionRequest request)
    {
        try
        {
            var adminIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdString, out int adminId)) return Unauthorized();

            var result = await _adminSubscriptionService.RevokePremiumSubscriptionAsync(id, request.AdminNote, adminId);
            return Ok(new { success = result, message = "Thu hồi gói Premium của người dùng thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// API lấy dữ liệu thống kê tổng quan các gói Premium.
    /// </summary>
    [RequirePermission("subscription.manage")]
    [HttpGet("stats")]
    public async Task<IActionResult> GetSubscriptionStats()
    {
        try
        {
            var stats = await _adminSubscriptionService.GetSubscriptionStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

/// <summary>
/// Request DTO cho việc thu hồi Premium.
/// </summary>
public class RevokeSubscriptionRequest
{
    public string? AdminNote { get; set; }
}
