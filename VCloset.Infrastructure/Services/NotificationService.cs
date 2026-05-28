using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCloset.Application.DTOs;
using VCloset.Application.DTOs.Admin.Requests;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Domain.Enums;

namespace VCloset.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationHubService _hubService;

    public NotificationService(IUnitOfWork unitOfWork, INotificationHubService hubService)
    {
        _unitOfWork = unitOfWork;
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

        await _unitOfWork.Notifications.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();

        // Real-time Push Alert: Gửi số lượng tin nhắn chưa đọc mới cho Flutter
        var newCount = await GetUnreadCountAsync(userId);
        await _hubService.SendUnreadCountAlertAsync(userId, newCount);

        return MapToDto(notification);
    }

    public async Task<List<NotificationResponseDto>> GetUserNotificationsAsync(int userId, bool? isRead)
    {
        var notifications = await _unitOfWork.Notifications.FindAllAsync(n => n.UserInternalId == userId);
        
        var query = notifications.AsQueryable();

        if (isRead.HasValue)
        {
            query = query.Where(n => n.IsRead == isRead.Value);
        }

        var sortedNotifications = query
            .OrderByDescending(n => n.CreatedAt)
            .ToList();

        return sortedNotifications.Select(MapToDto).ToList();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        var unread = await _unitOfWork.Notifications.FindAllAsync(n => n.UserInternalId == userId && !n.IsRead);
        return unread.Count();
    }

    public async Task<bool> MarkAsReadAsync(int userId, Guid notificationId)
    {
        var notification = await _unitOfWork.Notifications.FindAsync(n => n.Id == notificationId && n.UserInternalId == userId);

        if (notification == null) return false;

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            _unitOfWork.Notifications.Update(notification);
            await _unitOfWork.SaveChangesAsync();

            // Real-time Push Alert: Cập nhật giảm số lượng tin chưa đọc
            var newCount = await GetUnreadCountAsync(userId);
            await _hubService.SendUnreadCountAlertAsync(userId, newCount);
        }

        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(int userId)
    {
        var unreadNotifications = await _unitOfWork.Notifications.FindAllAsync(n => n.UserInternalId == userId && !n.IsRead);

        if (!unreadNotifications.Any()) return true;

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            _unitOfWork.Notifications.Update(notification);
        }

        await _unitOfWork.SaveChangesAsync();

        // Real-time Push Alert: Gửi số lượng tin chưa đọc mới (= 0)
        await _hubService.SendUnreadCountAlertAsync(userId, 0);

        return true;
    }

    public async Task<bool> DeleteNotificationAsync(int userId, Guid notificationId)
    {
        var notification = await _unitOfWork.Notifications.FindAsync(n => n.Id == notificationId && n.UserInternalId == userId);

        if (notification == null) return false;

        bool wasUnread = !notification.IsRead;

        _unitOfWork.Notifications.Delete(notification);
        await _unitOfWork.SaveChangesAsync();

        // Real-time Push Alert: Cập nhật lại số lượng nếu xóa tin chưa đọc
        if (wasUnread)
        {
            var newCount = await GetUnreadCountAsync(userId);
            await _hubService.SendUnreadCountAlertAsync(userId, newCount);
        }

        return true;
    }

    public async Task SendBroadcastNotificationAsync(string type, string title, string body, string? referenceType, int? referenceId)
    {
        var users = await _unitOfWork.Users.FindAllAsync(u => u.Role == UserRole.Customer && u.IsActive);

        if (!users.Any()) return;

        foreach (var user in users)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserInternalId = user.InternalId,
                Type = type,
                Title = title,
                Body = body,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Notifications.AddAsync(notification);
        }

        await _unitOfWork.SaveChangesAsync();

        foreach (var user in users)
        {
            try
            {
                var newCount = await GetUnreadCountAsync(user.InternalId);
                await _hubService.SendUnreadCountAlertAsync(user.InternalId, newCount);
            }
            catch
            {
                // Bỏ qua lỗi gửi SignalR đơn lẻ nếu user offline hoặc ngắt kết nối
            }
        }
    }

    public async Task SaveDeviceTokenAsync(int userId, SaveDeviceTokenRequest request)
    {
        var existingToken = await _unitOfWork.UserDeviceTokens.FindAsync(t => t.FcmToken == request.FcmToken);

        if (existingToken != null)
        {
            existingToken.UserInternalId = userId;
            existingToken.DeviceType = request.DeviceType;
            existingToken.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.UserDeviceTokens.Update(existingToken);
        }
        else
        {
            var newToken = new UserDeviceToken
            {
                Id = Guid.NewGuid(),
                UserInternalId = userId,
                FcmToken = request.FcmToken,
                DeviceType = request.DeviceType,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.UserDeviceTokens.AddAsync(newToken);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<bool> BulkDeleteNotificationsAsync(int userId, List<Guid> notificationIds)
    {
        var toDelete = await _unitOfWork.Notifications.FindAllAsync(n => n.UserInternalId == userId && notificationIds.Contains(n.Id));

        if (!toDelete.Any()) return false;

        bool hasUnreadDeleted = false;

        foreach (var notification in toDelete)
        {
            if (!notification.IsRead)
            {
                hasUnreadDeleted = true;
            }
            _unitOfWork.Notifications.Delete(notification);
        }

        await _unitOfWork.SaveChangesAsync();

        if (hasUnreadDeleted)
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
