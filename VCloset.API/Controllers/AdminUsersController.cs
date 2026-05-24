using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using VCloset.Application.DTOs.Admin.Requests;
using VCloset.Application.Interfaces;
using VCloset.Infrastructure.Security;

namespace VCloset.API.Controllers;

[Route("api/admin/users")]
[ApiController]
[Authorize]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;

    public AdminUsersController(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }


    [RequirePermission("user.view")]
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? isBanned = null)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var result = await _adminUserService.GetUsersAsync(page, pageSize, search, role, isActive, isBanned);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [RequirePermission("user.view")]
    [HttpGet("{targetUserId:guid}")]
    public async Task<IActionResult> GetUserDetail(Guid targetUserId)
    {
        try
        {
            var result = await _adminUserService.GetUserDetailAsync(targetUserId);
            if (result == null) return NotFound("Không tìm thấy người dùng.");

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [RequirePermission("user.ban")]
    [HttpPost("{targetUserId:guid}/ban")]
    public async Task<IActionResult> BanUser(Guid targetUserId, [FromBody] BanUserRequest request)
    {
        try
        {
            var adminIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdString, out int adminId)) return Unauthorized();

            await _adminUserService.BanUserAsync(adminId, targetUserId, request);
            return Ok(new { Message = $"Đã khoá {request.BanType} của người dùng thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [RequirePermission("user.ban")]
    [HttpDelete("{targetUserId:guid}/ban")]
    public async Task<IActionResult> UnbanUser(Guid targetUserId, [FromQuery] string? liftReason = null)
    {
        try
        {
            var adminIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdString, out int adminId)) return Unauthorized();

            await _adminUserService.UnbanUserAsync(adminId, targetUserId, liftReason);
            return Ok(new { Message = "Đã gỡ ban người dùng thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [RequirePermission("user.deactivate")]
    [HttpDelete("{targetUserId:guid}")]
    public async Task<IActionResult> DeactivateUser(Guid targetUserId)
    {
        try
        {
            var adminIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdString, out int adminId)) return Unauthorized();

            await _adminUserService.DeactivateUserAsync(adminId, targetUserId);
            return Ok(new { Message = "Đã vô hiệu hoá tài khoản người dùng thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
