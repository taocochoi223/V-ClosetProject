using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VCloset.Application.DTOs;
using VCloset.Application.Interfaces;
using VCloset.Application.DTOs.Admin.Requests;
using VCloset.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace VCloset.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Produces("application/json")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationsController(INotificationService notificationService, IUnitOfWork unitOfWork)
    {
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Lấy danh sách thông báo của người dùng hiện tại (hỗ trợ lọc theo trạng thái isRead và phân trang)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotifications([FromQuery] bool? isRead, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();
        var notifications = await _notificationService.GetUserNotificationsAsync(userId, isRead, page, pageSize);
        return Ok(notifications);
    }

    /// <summary>
    /// Lấy số lượng thông báo chưa đọc (để hiển thị chấm đỏ trên icon quả chuông)
    /// </summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();
        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(new { count });
    }

    /// <summary>
    /// Đánh dấu một thông báo cụ thể là đã đọc
    /// </summary>
    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();
        var result = await _notificationService.MarkAsReadAsync(userId, id);
        if (!result) return NotFound(new { message = "Không tìm thấy thông báo hoặc thông báo không thuộc về người dùng này." });
        return NoContent();
    }

    /// <summary>
    /// Đánh dấu tất cả thông báo của người dùng hiện tại là đã đọc
    /// </summary>
    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();
        await _notificationService.MarkAllAsReadAsync(userId);
        return NoContent();
    }

    /// <summary>
    /// Xóa hoàn toàn một thông báo
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNotification(Guid id)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();
        var result = await _notificationService.DeleteNotificationAsync(userId, id);
        if (!result) return NotFound(new { message = "Không tìm thấy thông báo hoặc thông báo không thuộc về người dùng này." });
        return NoContent();
    }

    /// <summary>
    /// API của Admin để phát loa thông báo cho toàn bộ người dùng (Customer) trong hệ thống
    /// </summary>
    [HttpPost("broadcast")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> BroadcastNotification([FromBody] BroadcastNotificationRequest request)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

        var user = await _unitOfWork.Users.FindAsync(u => u.InternalId == userId);
        if (user == null || !user.IsActive || (user.Role != UserRole.Admin && user.Role != UserRole.Moderator))
        {
            return Forbid();
        }

        try
        {
            await _notificationService.SendBroadcastNotificationAsync(
                request.Type ?? "System",
                request.Title,
                request.Body,
                request.ReferenceType,
                request.ReferenceId
            );
            return Ok(new { message = "Đã phát loa thông báo thành công tới toàn bộ người dùng." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// API của Admin để gửi thông báo riêng biệt tới 1 người dùng cụ thể
    /// </summary>
    [HttpPost("admin/send-to-user")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SendTargetedNotification([FromBody] SendTargetedNotificationRequest request)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

        var user = await _unitOfWork.Users.FindAsync(u => u.InternalId == userId);
        if (user == null || !user.IsActive || (user.Role != UserRole.Admin && user.Role != UserRole.Moderator))
        {
            return Forbid();
        }

        try
        {
            var targetUser = await _unitOfWork.Users.FindAsync(u => u.InternalId == request.UserId);
            if (targetUser == null)
            {
                return BadRequest(new { message = "Không tìm thấy người dùng nhận thông báo." });
            }

            var result = await _notificationService.SendNotificationAsync(
                request.UserId,
                request.Type ?? "System",
                request.Title,
                request.Body,
                request.ReferenceType,
                request.ReferenceId
            );
            return Ok(new { message = "Gửi thông báo thành công.", data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// API của Admin/Moderator để kiểm tra, giám sát toàn bộ thông báo đã phát ra trong hệ thống
    /// </summary>
    [HttpGet("admin/all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllNotifications(
        [FromQuery] int? targetUserId,
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

        var user = await _unitOfWork.Users.FindAsync(u => u.InternalId == userId);
        if (user == null || !user.IsActive || (user.Role != UserRole.Admin && user.Role != UserRole.Moderator))
        {
            return Forbid();
        }

        var results = await _notificationService.GetAllNotificationsForAdminAsync(targetUserId, type, page, pageSize);
        return Ok(results);
    }

    /// <summary>
    /// API của Admin/Moderator để xóa cưỡng chế hoặc thu hồi thông báo khỏi hệ thống
    /// </summary>
    [HttpDelete("admin/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteNotificationByAdmin(Guid id)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

        var user = await _unitOfWork.Users.FindAsync(u => u.InternalId == userId);
        if (user == null || !user.IsActive || (user.Role != UserRole.Admin && user.Role != UserRole.Moderator))
        {
            return Forbid();
        }

        var result = await _notificationService.DeleteNotificationByAdminAsync(id);
        if (!result)
        {
            return NotFound(new { message = "Không tìm thấy thông báo cần xóa." });
        }

        return NoContent();
    }

    /// <summary>
    /// API Lưu Token thiết bị FCM của người dùng để đẩy thông báo qua Firebase khi tắt ứng dụng
    /// </summary>
    [HttpPost("device-token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SaveDeviceToken([FromBody] SaveDeviceTokenRequest request)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

        await _notificationService.SaveDeviceTokenAsync(userId, request);
        return Ok(new { message = "Lưu token thiết bị thành công." });
    }

    /// <summary>
    /// API Xóa hàng loạt thông báo được chọn (hỗ trợ tích chọn nhiều thông báo trên app)
    /// </summary>
    [HttpPost("bulk-delete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> BulkDeleteNotifications([FromBody] BulkDeleteNotificationsRequest request)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

        if (request.NotificationIds == null || request.NotificationIds.Count == 0)
        {
            return BadRequest(new { message = "Danh sách ID thông báo cần xóa không được để trống." });
        }

        var result = await _notificationService.BulkDeleteNotificationsAsync(userId, request.NotificationIds);
        if (!result)
        {
            return BadRequest(new { message = "Không tìm thấy thông báo nào được chỉ định thuộc về bạn để xóa." });
        }

        return Ok(new { message = "Đã xóa hàng loạt thông báo thành công." });
    }
}
