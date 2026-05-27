using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VCloset.Application.DTOs;
using VCloset.Application.Interfaces;
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
    /// API Hỗ trợ Test (Sandbox): Tự tạo một thông báo giả lập cho người dùng hiện tại
    /// </summary>
    [HttpPost("send-test")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> SendTestNotification([FromQuery] string type, [FromQuery] string title, [FromQuery] string body, [FromQuery] string? referenceType, [FromQuery] int? referenceId)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int userId)) return Unauthorized();
        var result = await _notificationService.SendNotificationAsync(
            userId, 
            type ?? "System", 
            title ?? "Thông báo giả lập", 
            body ?? "Đây là nội dung tin nhắn giả lập để hỗ trợ test kết nối Flutter.", 
            referenceType, 
            referenceId
        );
        return CreatedAtAction(nameof(GetNotifications), null, result);
    }
}
