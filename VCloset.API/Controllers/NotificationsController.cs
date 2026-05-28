using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VCloset.Application.DTOs;
using VCloset.Application.Interfaces;
using VCloset.Application.DTOs.Admin.Requests;
using VCloset.Infrastructure.Security;
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

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Lấy danh sách thông báo của người dùng hiện tại (hỗ trợ lọc theo trạng thái isRead)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotifications([FromQuery] bool? isRead)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();
        var notifications = await _notificationService.GetUserNotificationsAsync(userId, isRead);
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
    [RequirePermission("notification.broadcast")]
    [HttpPost("broadcast")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> BroadcastNotification([FromBody] BroadcastNotificationRequest request)
    {
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
