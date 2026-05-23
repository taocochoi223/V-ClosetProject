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

    public NotificationService(VClosetVersion30Context context)
    {
        _context = context;
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
        return true;
    }

    public async Task<bool> DeleteNotificationAsync(int userId, Guid notificationId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserInternalId == userId);

        if (notification == null) return false;

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();
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
