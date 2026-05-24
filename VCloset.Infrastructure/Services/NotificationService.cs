using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VCloset.Application.DTOs;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Infrastructure.Data;

namespace VCloset.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly VClosetVersion30Context _context;
    private readonly INotificationHubService _hubService;

    public NotificationService(VClosetVersion30Context context, INotificationHubService hubService)
    {
        _context = context;
        _hubService = hubService;
    }

    public async Task<NotificationResponseDto> SendNotificationAsync(int userId, string type, string title, string body, string? referenceType, int? referenceId)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserInternalId = userId,
            Type = type,
            Title = title,
            Body = body,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Real-time Push Alert: Gửi số lượng tin nhắn chưa đọc mới cho Flutter
        var newCount = await GetUnreadCountAsync(userId);
        await _hubService.SendUnreadCountAlertAsync(userId, newCount);

        return MapToDto(notification);
    }

    public async Task<List<NotificationResponseDto>> GetUserNotificationsAsync(int userId, bool? isRead)
    {
        var query = _context.Notifications
            .Where(n => n.UserInternalId == userId);

        if (isRead.HasValue)
        {
            query = query.Where(n => n.IsRead == isRead.Value);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notifications.Select(MapToDto).ToList();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserInternalId == userId && !n.IsRead);
    }

    public async Task<bool> MarkAsReadAsync(int userId, Guid notificationId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserInternalId == userId);

        if (notification == null) return false;

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();

            // Real-time Push Alert: Cập nhật giảm số lượng tin chưa đọc
            var newCount = await GetUnreadCountAsync(userId);
            await _hubService.SendUnreadCountAlertAsync(userId, newCount);
        }

        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(int userId)
    {
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserInternalId == userId && !n.IsRead)
            .ToListAsync();

        if (!unreadNotifications.Any()) return true;

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync();

        // Real-time Push Alert: Gửi số lượng tin chưa đọc mới (= 0)
        await _hubService.SendUnreadCountAlertAsync(userId, 0);

        return true;
    }

    public async Task<bool> DeleteNotificationAsync(int userId, Guid notificationId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserInternalId == userId);

        if (notification == null) return false;

        bool wasUnread = !notification.IsRead;

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();

        // Real-time Push Alert: Cập nhật lại số lượng nếu xóa tin chưa đọc
        if (wasUnread)
        {
            var newCount = await GetUnreadCountAsync(userId);
            await _hubService.SendUnreadCountAlertAsync(userId, newCount);
        }

        return true;
    }

    private static NotificationResponseDto MapToDto(Notification entity)
    {
        return new NotificationResponseDto
        {
            Id = entity.Id,
            Type = entity.Type,
            Title = entity.Title,
            Body = entity.Body,
            ReferenceType = entity.ReferenceType,
            ReferenceId = entity.ReferenceId,
            IsRead = entity.IsRead,
            CreatedAt = entity.CreatedAt
        };
    }
}
