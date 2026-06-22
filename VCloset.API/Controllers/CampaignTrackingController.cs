using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VCloset.Application.Interfaces;

namespace VCloset.API.Controllers;

[ApiController]
[Route("api/v1/campaigns")]
public class CampaignTrackingController : ControllerBase
{
    private readonly ICampaignTrackingService _trackingService;

    public CampaignTrackingController(ICampaignTrackingService trackingService)
    {
        _trackingService = trackingService;
    }

    /// <summary>
    /// Ghi nhận 1 lượt hiển thị (Impression) của quảng cáo
    /// Client (App/Web) sẽ gọi API này khi một thẻ quảng cáo xuất hiện trên màn hình của người dùng.
    /// </summary>
    [HttpPost("{id}/impression")]
    [AllowAnonymous]
    public async Task<IActionResult> RecordImpression(Guid id)
    {
        try
        {
            await _trackingService.RecordImpressionAsync(id);
            return Ok(new { success = true, message = "Impression recorded." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Ghi nhận 1 lượt nhấp (Click) của quảng cáo
    /// Client (App/Web) sẽ gọi API này khi người dùng click vào quảng cáo. 
    /// Lượt click này sẽ tự động bị tính phí và trừ tiền vào tài khoản Brand.
    /// </summary>
    [HttpPost("{id}/click")]
    [AllowAnonymous]
    public async Task<IActionResult> RecordClick(Guid id)
    {
        try
        {
            // Mặc định giả định chi phí là 1,000 VND cho 1 click.
            // Có thể nâng cấp để lấy giá trị CPC tùy theo từng cấu hình chiến dịch sau này.
            await _trackingService.RecordClickAsync(id, 1000m);
            return Ok(new { success = true, message = "Click recorded and charged." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
