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
    [RequirePermission("dashboard.view")]
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
    /// Lấy dữ liệu biểu đồ doanh thu VIP Premium theo khoảng thời gian tùy chọn (week hoặc month)
    /// </summary>
    [RequirePermission("dashboard.view")]
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
}
