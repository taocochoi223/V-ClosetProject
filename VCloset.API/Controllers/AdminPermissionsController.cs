using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VCloset.Application.DTOs.AdminPermissions.Requests;
using VCloset.Application.Interfaces;

namespace VCloset.API.Controllers;

/// <summary>
/// Quản lý phân quyền (Permissions) cho các tài khoản Admin/Moderator.
/// </summary>
[Route("api/admin/permissions")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminPermissionsController : ControllerBase
{
    private readonly IAdminPermissionService _adminPermissionService;
    private readonly ILogger<AdminPermissionsController> _logger;

    public AdminPermissionsController(IAdminPermissionService adminPermissionService, ILogger<AdminPermissionsController> logger)
    {
        _adminPermissionService = adminPermissionService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách toàn bộ Permission trong hệ thống. (Chỉ SuperAdmin)
    /// </summary>
    [HttpGet("all")]
    [Authorize(Policy = "RequirePermission:permission.grant")]
    public async Task<IActionResult> GetAllPermissions()
    {
        try
        {
            var result = await _adminPermissionService.GetAllPermissionsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách permissions.");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách quyền hiện tại của 1 Admin User.
    /// </summary>
    [HttpGet("{userId:guid}")]
    [Authorize(Policy = "RequirePermission:permission.grant")]
    public async Task<IActionResult> GetUserPermissions(Guid userId)
    {
        try
        {
            var result = await _adminPermissionService.GetUserPermissionsAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi lấy danh sách permissions của user {userId}.");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách quyền hiện tại của chính bản thân (dùng cho FrontEnd render Menu).
    /// </summary>
    [HttpGet("me")]
    // Không cần Policy RequirePermission, vì ai cũng có quyền xem quyền của chính mình
    public async Task<IActionResult> GetMyPermissions()
    {
        try
        {
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdStr, out int currentUserId)) return Unauthorized();

            var result = await _adminPermissionService.GetUserPermissionsByInternalIdAsync(currentUserId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách permissions của bản thân.");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cấp thêm quyền cho 1 Admin User.
    /// </summary>
    [HttpPost("{userId:guid}/grant")]
    [Authorize(Policy = "RequirePermission:permission.grant")]
    public async Task<IActionResult> GrantPermissions(Guid userId, [FromBody] UpdatePermissionRequest request)
    {
        try
        {
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdStr, out int currentUserId)) return Unauthorized();

            await _adminPermissionService.GrantPermissionsAsync(userId, request, currentUserId);
            return Ok(new { message = "Đã cấp thêm quyền thành công." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi cấp quyền cho user {userId}.");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Rút bớt quyền của 1 Admin User.
    /// </summary>
    [HttpPost("{userId:guid}/revoke")]
    [Authorize(Policy = "RequirePermission:permission.grant")]
    public async Task<IActionResult> RevokePermissions(Guid userId, [FromBody] UpdatePermissionRequest request)
    {
        try
        {
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdStr, out int currentUserId)) return Unauthorized();

            await _adminPermissionService.RevokePermissionsAsync(userId, request, currentUserId);
            return Ok(new { message = "Đã rút quyền thành công." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi rút quyền của user {userId}.");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Khôi phục toàn bộ quyền của User về mặc định theo Role của họ.
    /// </summary>
    [HttpPost("{userId:guid}/reset")]
    [Authorize(Policy = "RequirePermission:permission.grant")]
    public async Task<IActionResult> ResetToDefaultPermissions(Guid userId)
    {
        try
        {
            var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdStr, out int currentUserId)) return Unauthorized();

            await _adminPermissionService.ResetToDefaultPermissionsAsync(userId, currentUserId);
            return Ok(new { message = "Đã khôi phục quyền về mặc định thành công." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi reset quyền của user {userId}.");
            return BadRequest(new { message = ex.Message });
        }
    }
}
