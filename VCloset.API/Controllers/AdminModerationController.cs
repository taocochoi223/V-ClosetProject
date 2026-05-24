using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using VCloset.Application.DTOs.Admin.Requests;
using VCloset.Application.Interfaces;
using VCloset.Infrastructure.Security;

namespace VCloset.API.Controllers;

[Route("api/admin/moderation")]
[ApiController]
[Authorize]
public class AdminModerationController : ControllerBase
{
    private readonly IAdminModerationService _adminModerationService;

    public AdminModerationController(IAdminModerationService adminModerationService)
    {
        _adminModerationService = adminModerationService;
    }

    /// <summary>
    /// API lấy danh sách báo cáo vi phạm cộng đồng (Phân trang, lọc theo trạng thái xử lý/lý do)
    /// </summary>
    [RequirePermission("moderation.view")]
    [HttpGet("reports")]
    [ProducesResponseType(typeof(PagedReportsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetReports(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isResolved = null,
        [FromQuery] string? reason = null)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var result = await _adminModerationService.GetReportsAsync(page, pageSize, isResolved, reason);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// API xem thông tin chi tiết của một bài viết bị báo cáo vi phạm (ảnh canvas, lý do report)
    /// </summary>
    [RequirePermission("post.view")]
    [HttpGet("posts/{postId:guid}")]
    [ProducesResponseType(typeof(PostModerationDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPostDetail(Guid postId)
    {
        try
        {
            var result = await _adminModerationService.GetPostDetailForModerationAsync(postId);
            if (result == null) return NotFound(new { message = "Không tìm thấy bài viết hoặc bài đăng này." });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// API xử lý/giải quyết báo cáo vi phạm (ẩn bài viết hoặc bác bỏ báo cáo rác)
    /// </summary>
    [RequirePermission("moderation.resolve")]
    [HttpPost("reports/{reportId:guid}/resolve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResolveReport(Guid reportId, [FromBody] ResolveReportRequest request)
    {
        try
        {
            var adminIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdString, out int adminId)) return Unauthorized(new { message = "Không xác định được Admin từ token." });

            await _adminModerationService.ResolveReportAsync(adminId, reportId, request);
            return Ok(new { message = "Đã xử lý giải quyết báo cáo vi phạm thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// API thay đổi trạng thái ẩn/hiện hiển thị bài đăng của người dùng
    /// </summary>
    [RequirePermission("post.edit")]
    [HttpPut("posts/{postId:guid}/visibility")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetPostVisibility(Guid postId, [FromBody] PostVisibilityRequest request)
    {
        try
        {
            var adminIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdString, out int adminId)) return Unauthorized(new { message = "Không xác định được Admin từ token." });

            await _adminModerationService.SetPostVisibilityAsync(adminId, postId, request);
            string actionText = request.IsHidden ? "ẩn" : "hiển thị lại";
            return Ok(new { message = $"Đã thay đổi trạng thái bài viết thành {actionText} thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
