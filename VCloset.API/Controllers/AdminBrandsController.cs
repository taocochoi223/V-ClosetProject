using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using VCloset.Application.DTOs.Admin.Requests;
using VCloset.Application.Interfaces;
using VCloset.Domain.Enums;
using VCloset.Infrastructure.Security;

namespace VCloset.API.Controllers;

[Route("api/admin/brands")]
[ApiController]
[Authorize]
public class AdminBrandsController : ControllerBase
{
    private readonly IAdminBrandService _adminBrandService;

    public AdminBrandsController(IAdminBrandService adminBrandService)
    {
        _adminBrandService = adminBrandService;
    }

    /// <summary>
    /// API xem danh sách các đối tác thương hiệu (Brand Partner)
    /// </summary>
    [RequirePermission("brand.verify")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBrands(
        [FromQuery] BrandStatus? status = null,
        [FromQuery] string? search = null)
    {
        try
        {
            var result = await _adminBrandService.GetBrandsAsync(status, search);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// API cập nhật trạng thái hoạt động của đối tác thương hiệu (Duyệt Verified hoặc Đình chỉ Suspended)
    /// </summary>
    [RequirePermission("brand.verify")]
    [HttpPut("{brandId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateBrandStatus(Guid brandId, [FromBody] UpdateBrandStatusRequest request)
    {
        try
        {
            var adminIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdString, out int adminId)) return Unauthorized(new { message = "Không xác định được Admin từ token." });

            await _adminBrandService.UpdateBrandStatusAsync(adminId, brandId, request);
            string statusText = request.Status == BrandStatus.Verified ? "phê duyệt thành công" : "đình chỉ hoạt động";
            return Ok(new { message = $"Đã {statusText} thương hiệu thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// API nạp tiền quảng cáo (tín dụng) vào số dư của đối tác thương hiệu
    /// </summary>
    [RequirePermission("brand.verify")]
    [HttpPost("{brandId:guid}/credit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RechargeBrandCredit(Guid brandId, [FromBody] RechargeBrandCreditRequest request)
    {
        try
        {
            var adminIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdString, out int adminId)) return Unauthorized(new { message = "Không xác định được Admin từ token." });

            await _adminBrandService.RechargeBrandCreditAsync(adminId, brandId, request);
            return Ok(new { message = $"Đã nạp {request.Amount:N2}đ vào tài khoản quảng cáo của thương hiệu thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
