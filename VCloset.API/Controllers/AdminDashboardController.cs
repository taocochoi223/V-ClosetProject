using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using VCloset.Application.Interfaces;
using VCloset.Infrastructure.Security;

namespace VCloset.API.Controllers;

[Route("api/admin/dashboard")]
[ApiController]
[Authorize]
public class AdminDashboardController : ControllerBase
{
    private readonly IAdminDashboardService _adminDashboardService;

    public AdminDashboardController(IAdminDashboardService adminDashboardService)
    {
        _adminDashboardService = adminDashboardService;
    }

    /// <summary>
    /// Lấy toàn bộ số liệu thống kê tổng hợp (KPI Metrics) của hệ thống
    /// </summary>
    [RequirePermission("analytics.view")]
    [HttpGet("metrics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMetrics()
    {
        try
        {
            var result = await _adminDashboardService.GetDashboardMetricsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy dữ liệu biểu đồ Doanh thu Premium vs Hoa hồng Affiliate theo khoảng thời gian (week hoặc month)
    /// </summary>
    [RequirePermission("analytics.view")]
    [HttpGet("revenue-chart")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRevenueChart([FromQuery] string period = "month")
    {
        try
        {
            var result = await _adminDashboardService.GetRevenueChartDataAsync(period);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách người dùng mới đăng ký gần đây (cho section "Đăng ký mới gần đây")
    /// </summary>
    [RequirePermission("analytics.view")]
    [HttpGet("recent-signups")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRecentSignups([FromQuery] int limit = 5)
    {
        try
        {
            var result = await _adminDashboardService.GetRecentSignupsAsync(limit);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy bảng tin hệ thống và cảnh báo API theo thời gian thực (cho section "Bảng tin hệ thống &amp; Cảnh báo API")
    /// </summary>
    [RequirePermission("analytics.view")]
    [HttpGet("system-alerts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSystemAlerts()
    {
        try
        {
            var result = await _adminDashboardService.GetSystemAlertsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Xuất báo cáo tổng hợp Dashboard (Premium + Affiliate) ra file CSV UTF-8
    /// </summary>
    [RequirePermission("analytics.view")]
    [HttpGet("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        try
        {
            var fileBytes = await _adminDashboardService.ExportDashboardReportAsync(from, to);
            var fileName = $"dashboard-report-{DateTime.Now:yyyyMMddHHmmss}.csv";
            return File(fileBytes, "text/csv; charset=utf-8", fileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy dữ liệu phân tích nhân khẩu học dựa trên khảo sát Onboarding của người dùng
    /// </summary>
    [RequirePermission("analytics.view")]
    [HttpGet("onboarding-demographics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOnboardingDemographics()
    {
        try
        {
            var result = await _adminDashboardService.GetOnboardingDemographicsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

