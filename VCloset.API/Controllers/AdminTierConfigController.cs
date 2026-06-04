using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VCloset.Application.DTOs.TierConfig;
using VCloset.Application.Interfaces;
using VCloset.Infrastructure.Security;

namespace VCloset.API.Controllers;

/// <summary>
/// Quản lý cấu hình giới hạn theo tier (Free / Premium).
/// Admin có thể chỉnh số lượt AI, giới hạn tủ đồ và phối đồ mà không cần deploy lại.
/// </summary>
[Route("api/admin/tier-config")]
[ApiController]
[Authorize]
[RequirePermission("subscription.manage")]
public class AdminTierConfigController : ControllerBase
{
    private readonly ITierConfigService _tierConfigService;
    private readonly ILogger<AdminTierConfigController> _logger;

    public AdminTierConfigController(ITierConfigService tierConfigService, ILogger<AdminTierConfigController> logger)
    {
        _tierConfigService = tierConfigService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy cấu hình giới hạn của tất cả các tier (free, premium).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var result = await _tierConfigService.GetAllAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách tier config.");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy cấu hình giới hạn của 1 tier cụ thể.
    /// tierName: "free" hoặc "premium"
    /// </summary>
    [HttpGet("{tierName}")]
    public async Task<IActionResult> GetByTier(string tierName)
    {
        try
        {
            var result = await _tierConfigService.GetByTierAsync(tierName);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy tier config cho {Tier}.", tierName);
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật cấu hình giới hạn cho 1 tier.
    /// tierName: "free" hoặc "premium"
    /// Body: { bgRemovalCredits, tryOnCredits, wardrobeItemLimit (null = unlimited), outfitLimit (null = unlimited) }
    /// </summary>
    [HttpPut("{tierName}")]
    public async Task<IActionResult> Update(string tierName, [FromBody] UpdateTierConfigRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var adminEmail = User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? "unknown";

            var result = await _tierConfigService.UpdateAsync(tierName, request, adminEmail);
            _logger.LogInformation("Admin {Admin} đã cập nhật tier config cho {Tier}.", adminEmail, tierName);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật tier config cho {Tier}.", tierName);
            return BadRequest(new { message = ex.Message });
        }
    }
}
