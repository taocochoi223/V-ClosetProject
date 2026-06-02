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

    /// <summary>
    /// API lấy danh sách người dùng phân trang (Admin/Moderator). 
    /// Lọc theo tìm kiếm (Email, Tên hiển thị), vai trò, trạng thái kích hoạt, trạng thái bị ban.
    /// Giới hạn hiển thị: Chỉ thấy những tài khoản có cấp bậc thấp hơn người gọi.
    /// </summary>
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

            var adminIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdString, out int adminId)) return Unauthorized();

            var result = await _adminUserService.GetUsersAsync(adminId, page, pageSize, search, role, isActive, isBanned);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// API xem chi tiết người dùng (Admin/Moderator). 
    /// Trả về hồ sơ cơ bản, số đo mannequin và lịch sử khóa (ban log) của user đó.
    /// </summary>
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

    /// <summary>
    /// API khóa quyền của người dùng (Tạm thời hoặc Vĩnh viễn).
    /// Hỗ trợ ban loại: "chat" (khóa chat), "post" (khóa đăng bài), "all" (khóa toàn bộ).
    /// </summary>
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

    /// <summary>
    /// API gỡ khóa quyền của người dùng.
    /// Cho phép kích hoạt lại các quyền chat/đăng bài/tài khoản đang bị khóa.
    /// </summary>
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

    /// <summary>
    /// API vô hiệu hóa (deactivate) tài khoản người dùng (is_active = false).
    /// Chỉ có SuperAdmin mới được vô hiệu hóa tài khoản của các Admin khác.
    /// </summary>
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

    /// <summary>
    /// API tạo tài khoản người dùng trực tiếp từ Admin (Admin/SuperAdmin).
    /// Hỗ trợ tạo các vai trò: Customer, Admin, Moderator, BrandPartner.
    /// Hệ thống tự động sinh mật khẩu ngẫu nhiên gửi về email và gán quyền mặc định tương ứng.
    /// </summary>
    [RequirePermission("admin.create")]
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var adminIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdString, out int adminId)) return Unauthorized();

            await _adminUserService.CreateUserWithPermissionsAsync(adminId, request);
            return Ok(new { Message = "Tạo tài khoản thành công. Mật khẩu tạm thời đã được gửi tới email của người dùng." });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    /// <summary>
    /// API cập nhật vai trò (Role) của người dùng (Yêu cầu SuperAdmin).
    /// Hệ thống tự động chuyển đổi Profile và gán các quyền mặc định tương ứng với vai trò mới.
    /// </summary>
    [RequirePermission("admin.create")]
    [HttpPut("{targetUserId:guid}/role")]
    public async Task<IActionResult> UpdateUserRole(Guid targetUserId, [FromBody] UpdateRoleRequest request)
    {
        try
        {
            var adminIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdString, out int adminId)) return Unauthorized();

            await _adminUserService.UpdateUserRoleAsync(adminId, targetUserId, request.NewRole);
            return Ok(new { Message = $"Đã cập nhật vai trò của người dùng thành {request.NewRole} thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// API kích hoạt lại tài khoản người dùng đang bị vô hiệu hóa (Admin/SuperAdmin).
    /// Chỉ SuperAdmin mới được phép mở khóa/kích hoạt lại tài khoản của Admin khác.
    /// </summary>
    [RequirePermission("user.deactivate")]
    [HttpPost("{targetUserId:guid}/reactivate")]
    public async Task<IActionResult> ReactivateUser(Guid targetUserId)
    {
        try
        {
            var adminIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdString, out int adminId)) return Unauthorized();

            await _adminUserService.ReactivateUserAsync(adminId, targetUserId);
            return Ok(new { Message = "Đã kích hoạt lại tài khoản người dùng thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
