using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VCloset.Application.DTOs.Subscriptions.Requests;
using VCloset.Application.Interfaces;
using VCloset.Infrastructure.Security;

namespace VCloset.API.Controllers;

/// <summary>
/// Quản lý các gói Subscription Premium dành cho Admin.
/// </summary>
[Route("api/admin/subscriptions/plans")]
[ApiController]
[RequirePermission("subscription.manage")]
public class AdminSubscriptionsController : ControllerBase
{
    private readonly IAdminSubscriptionService _adminSubscriptionService;
    private readonly ILogger<AdminSubscriptionsController> _logger;

    public AdminSubscriptionsController(IAdminSubscriptionService adminSubscriptionService, ILogger<AdminSubscriptionsController> logger)
    {
        _adminSubscriptionService = adminSubscriptionService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách toàn bộ các gói Premium (kể cả Active và Inactive).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllPlans()
    {
        try
        {
            var plans = await _adminSubscriptionService.GetAllPlansAsync();
            return Ok(plans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách gói Premium.");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy chi tiết 1 gói Premium theo ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPlanById(Guid id)
    {
        try
        {
            var plan = await _adminSubscriptionService.GetPlanByIdAsync(id);
            return Ok(plan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi lấy chi tiết gói Premium ID {id}.");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Tạo mới một gói Premium.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreatePlan([FromBody] CreateOrUpdatePlanRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var plan = await _adminSubscriptionService.CreatePlanAsync(request);
            return Ok(plan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo gói Premium.");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật thông tin gói Premium.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdatePlanRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var plan = await _adminSubscriptionService.UpdatePlanAsync(id, request);
            return Ok(plan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi cập nhật gói Premium ID {id}.");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Ẩn/Xóa (Soft delete) một gói Premium.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePlan(Guid id)
    {
        try
        {
            var result = await _adminSubscriptionService.DeletePlanAsync(id);
            return Ok(new { success = result, message = "Đã vô hiệu hoá (ẩn) gói thành công." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi xóa gói Premium ID {id}.");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("grant")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GrantSubscription([FromBody] GrantSubscriptionRequest request)
    {
        try
        {
            var result = await _adminSubscriptionService.GrantSubscriptionToUserAsync(request);
            return Ok(new { success = result, message = "Đã tặng gói dịch vụ thành công." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tặng gói dịch vụ cho người dùng.");
            return BadRequest(new { message = ex.Message });
        }
    }
}
