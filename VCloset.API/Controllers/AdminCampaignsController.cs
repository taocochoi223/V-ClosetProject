using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using VCloset.Application.DTOs.Admin.Requests;
using VCloset.Application.Interfaces;
using VCloset.Infrastructure.Security;

namespace VCloset.API.Controllers;

[Route("api/admin/campaigns")]
[ApiController]
[Authorize]
public class AdminCampaignsController : ControllerBase
{
    private readonly IAdminBrandService _adminBrandService;

    public AdminCampaignsController(IAdminBrandService adminBrandService)
    {
        _adminBrandService = adminBrandService;
    }

    /// <summary>
    /// API lấy danh sách toàn bộ chiến dịch quảng cáo tài trợ đang chạy trên hệ thống
    /// </summary>
    [RequirePermission("analytics.view")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCampaigns()
    {
        try
        {
            var result = await _adminBrandService.GetCampaignsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// API yêu cầu dừng khẩn cấp một chiến dịch quảng cáo vi phạm tiêu chuẩn
    /// </summary>
    [RequirePermission("content.moderate")]
    [HttpPost("{campaignId:guid}/stop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> StopCampaign(Guid campaignId)
    {
        try
        {
            var adminIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdString, out int adminId)) return Unauthorized(new { message = "Không xác định được Admin từ token." });

            await _adminBrandService.StopCampaignAsync(adminId, campaignId);
            return Ok(new { message = "Đã dừng khẩn cấp chiến dịch quảng cáo vi phạm thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// API yêu cầu khôi phục/kích hoạt lại một chiến dịch quảng cáo đã dừng
    /// </summary>
    [RequirePermission("content.moderate")]
    [HttpPost("{campaignId:guid}/resume")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResumeCampaign(Guid campaignId)
    {
        try
        {
            var adminIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdString, out int adminId)) return Unauthorized(new { message = "Không xác định được Admin từ token." });

            await _adminBrandService.ResumeCampaignAsync(adminId, campaignId);
            return Ok(new { message = "Đã khôi phục/kích hoạt lại chiến dịch quảng cáo thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// API điều chỉnh ngân sách ngày và thứ hạng hiển thị của chiến dịch quảng cáo
    /// </summary>
    [RequirePermission("content.moderate")]
    [HttpPut("{campaignId:guid}/adjust")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AdjustCampaign(Guid campaignId, [FromBody] AdjustCampaignRequest request)
    {
        try
        {
            var adminIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdString, out int adminId)) return Unauthorized(new { message = "Không xác định được Admin từ token." });

            await _adminBrandService.AdjustCampaignAsync(adminId, campaignId, request);
            return Ok(new { message = "Đã điều chỉnh thông tin chiến dịch quảng cáo thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// API xóa hoặc lưu trữ một chiến dịch quảng cáo
    /// </summary>
    [RequirePermission("content.moderate")]
    [HttpDelete("{campaignId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteCampaign(Guid campaignId)
    {
        try
        {
            var adminIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdString, out int adminId)) return Unauthorized(new { message = "Không xác định được Admin từ token." });

            await _adminBrandService.DeleteCampaignAsync(adminId, campaignId);
            return Ok(new { message = "Đã xóa chiến dịch quảng cáo thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// API tìm kiếm, phân trang và sắp xếp chiến dịch quảng cáo tài trợ
    /// </summary>
    [RequirePermission("analytics.view")]
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchCampaigns(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] string? sortBy,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _adminBrandService.SearchCampaignsAsync(search, isActive, sortBy, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// API xuất báo cáo danh sách chiến dịch dưới dạng file CSV UTF-8
    /// </summary>
    [RequirePermission("analytics.view")]
    [HttpGet("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportCampaignsReport()
    {
        try
        {
            var fileBytes = await _adminBrandService.ExportCampaignsReportAsync();
            var fileName = $"campaigns-report-{DateTime.Now:yyyyMMddHHmmss}.csv";
            return File(fileBytes, "text/csv; charset=utf-8", fileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
