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
    [RequirePermission("campaign.manage")]
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
    [RequirePermission("campaign.manage")]
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
}
